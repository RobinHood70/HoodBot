#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static RobinHood70.Globals;

	internal class ContinueModule1 : ContinueModuleBase
	{
		#region Public Constants
		internal const string Name = "query-continue";
		#endregion

		#region Fields
		private string currentGeneratorValue;
		private string savedGeneratorValue;
		private SortedSet<string> modules = new SortedSet<string>();
		#endregion

		#region Public Override Methods
		public override void OnSubmit(IPageSetInternal pageSet)
		{
			base.OnSubmit(pageSet);
			if (pageSet is IQueryPageSet queryPageSet)
			{
				queryPageSet.DisabledModules.Clear();
				queryPageSet.DisabledModules.UnionWith(this.modules);
			}
		}
		#endregion

		#region Protected Override Methods
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
			if (parent != null)
			{
				// True by default for cases when there's no query-continue in the result and therefore DeserializeMain() isn't called.
				this.BatchComplete = true;
				var result = parent[Name];
				if (result != null && result.Type != JTokenType.Null)
				{
					this.Continues = true;
					this.ContinueEntries.Clear();
					this.modules.Clear();
#pragma warning disable IDE0007 // Use implicit type
					foreach (JProperty module in result)
					{
						foreach (JProperty param in module.Value)
#pragma warning restore IDE0007 // Use implicit type
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
				}
			}

			return 0;
		}
		#endregion
	}
}