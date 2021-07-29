namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	// MWVERSION: 1.27
	internal sealed class ActionHelp : ActionModule<HelpInput, HelpResult>
	{
		#region Constructors
		public ActionHelp(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 0;

		public override string Name => "help";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType => RequestType.Get;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, HelpInput input)
		{
			if (this.SiteVersion < 117)
			{
				return;
			}

			request.ThrowNull(nameof(request));
			input.ThrowNull(nameof(input));
			if (this.SiteVersion < 121)
			{
				var modules = new SortedSet<string>(StringComparer.Ordinal);
				var queryModules = new SortedSet<string>(StringComparer.Ordinal);
				foreach (var module in input.Modules)
				{
					var submodule = module.Split(TextArrays.Plus, 2, StringSplitOptions.None);
					if (submodule.Length == 2 && string.Equals(submodule[0].Trim(), "query", StringComparison.Ordinal))
					{
						queryModules.Add(submodule[1].Trim());
					}
					else
					{
						modules.Add(module.Trim());
					}
				}

				request
					.Add("modules", modules)
					.Add("querymodules", queryModules);
			}
			else
			{
				request
					.Add("modules", input.Modules)
					.AddIf("recursivesubmodules", input.RecursiveSubModules, this.SiteVersion >= 125)
					.AddIf("submodules", input.SubModules, this.SiteVersion >= 125)
					.AddIf("toc", input.Toc, this.SiteVersion >= 125)
					.AddIf("wrap", true, this.SiteVersion >= 125);
			}
		}

		protected override HelpResult DeserializeCustom(string? result)
		{
			result.ThrowNull(nameof(result));
			return new HelpResult(
				help: new List<string> { result },
				mime: "text/html");
		}

		protected override HelpResult DeserializeResult(JToken? result)
		{
			// Conceivably, results could be parsed more here (e.g., format 1.21-1.24 the same as 1.25+, and to get specific modules for MW 1.16-) but this has not been implemented due to the extreme unlikelihood of this module ever being used outside of very specific circumstances where the user is probably doing their own parsing anyway.
			result.ThrowNull(nameof(result));
			return result.Type == JTokenType.Array
				? new HelpResult(result.GetList<string>(), "text/html")
				: new HelpResult(new List<string> { result.MustHaveString("help") }, result.MustHaveString("mime"));
		}
		#endregion
	}
}
