namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WikiCommon.RequestBuilder;

	internal sealed class ContinueModule1 : ContinueModule
	{
		#region Public Constants
		public const string ContinueName = "query-continue";
		#endregion

		#region Fields
		private readonly HashSet<string> modules = new(System.StringComparer.Ordinal);

		private string? currentGeneratorValue;
		private string? savedGeneratorValue;
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 109;

		public override string Name => ContinueName;
		#endregion

		#region Public Override Methods
		public override void BuildRequest(Request request)
		{
			request.ThrowNull(nameof(request));

			// Check if query continue type has been set manually or a previous result did not emit a query-continue.
			if (this.Continues)
			{
				this.GeneratorContinue.ThrowNull(nameof(ContinueModule1), nameof(this.GeneratorContinue));

				// We must allow for changing, since some query-continues re-use parameters that may have already been added by the module.
				request.AddOrChangeIfNotNull(this.GeneratorContinue!, this.currentGeneratorValue);
				foreach (var entry in this.ContinueEntries)
				{
					request.AddOrChangeIfNotNull(entry.Key, entry.Value);
				}

				this.Continues = false;
			}
		}

		public override ContinueModule Deserialize(WikiAbstractionLayer wal, JToken parent)
		{
			var result = parent?[this.Name];
			if (result == null || result.Type == JTokenType.Null)
			{
				return this;
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
					if (string.Equals(param.Name, this.GeneratorContinue, System.StringComparison.Ordinal))
					{
						this.savedGeneratorValue = (string?)param.Value;
					}
					else
					{
						if (this.GeneratorContinue?.Length > 0)
						{
							this.BatchComplete = false;
						}

						this.modules.Add(module.Name);
						this.ContinueEntries[param.Name] = (string?)param.Value;
					}
				}
			}

			if (this.BatchComplete)
			{
				this.currentGeneratorValue = this.savedGeneratorValue;
			}

			return this;
		}
		#endregion

		#region Protected Internal Override Methods
		public override void BeforePageSetSubmit(IPageSetGenerator pageSet)
		{
			base.BeforePageSetSubmit(pageSet);
			if (pageSet is ActionQueryPageSet query)
			{
				query.InactiveModules.Clear();
				query.InactiveModules.UnionWith(this.modules);
			}
		}
		#endregion
	}
}