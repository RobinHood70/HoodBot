#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ContinueModule1 : ContinueModule
	{
		#region Public Constants
		public const string Name = "query-continue";
		#endregion

		#region Fields
		private readonly HashSet<string> modules = new HashSet<string>();

		private string currentGeneratorValue;
		private string savedGeneratorValue;
		#endregion

		#region Public Override Methods
		public override void BuildRequest(Request request)
		{
			ThrowNull(request, nameof(request));

			// Check if query continue type has been set manually or a previous result did not emit a query-continue.
			if (this.Continues)
			{
				// We must allow for changing, since some query-continues re-use parameters that may have already been added by the module.
				request.AddOrChangeIfNotNull(this.GeneratorContinue, this.currentGeneratorValue);
				foreach (var entry in this.ContinueEntries)
				{
					request.AddOrChangeIfNotNull(entry.Key, entry.Value);
				}

				this.Continues = false;
			}
		}

		public override int Deserialize(JToken parent)
		{
			var result = parent?[Name];
			if (result == null || result.Type == JTokenType.Null)
			{
				return 0;
			}

			// True by default for cases when there's no query-continue in the result and therefore DeserializeMain() isn't called.
			this.BatchComplete = true;
			this.Continues = true;
			this.ContinueEntries.Clear();
			this.modules.Clear();
			foreach (var module in result.Children<JProperty>())
			{
				foreach (var param in module.Value.Children<JProperty>())
				{
					if (param.Name == this.GeneratorContinue)
					{
						this.savedGeneratorValue = (string)param.Value;
					}
					else
					{
						if (this.GeneratorContinue.Length > 0)
						{
							this.BatchComplete = false;
						}

						this.modules.Add(module.Name);
						this.ContinueEntries[param.Name] = (string)param.Value;
					}
				}
			}

			if (this.BatchComplete)
			{
				this.currentGeneratorValue = this.savedGeneratorValue;
			}

			return 0;
		}
		#endregion

		#region Protected Internal Override Methods
		public override void BeforePageSetSubmit(IPageSetGenerator pageSet)
		{
			base.BeforePageSetSubmit(pageSet);
			if (pageSet is IQueryPageSet queryPageSet)
			{
				queryPageSet.InactiveModules.Clear();
				queryPageSet.InactiveModules.UnionWith(this.modules);
			}
		}
		#endregion
	}
}