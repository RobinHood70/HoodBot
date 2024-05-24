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
	internal sealed class ActionHelp(WikiAbstractionLayer wal) : ActionModule<HelpInput, HelpResult>(wal)
	{
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

			ArgumentNullException.ThrowIfNull(request);
			ArgumentNullException.ThrowIfNull(input);
			if (this.SiteVersion < 121)
			{
				SortedSet<string> modules = new(StringComparer.Ordinal);
				SortedSet<string> queryModules = new(StringComparer.Ordinal);
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
			ArgumentNullException.ThrowIfNull(result);
			return new HelpResult(
				help: [result],
				mime: "text/html");
		}

		protected override HelpResult DeserializeResult(JToken? result)
		{
			// Conceivably, results could be parsed more here (e.g., format 1.21-1.24 the same as 1.25+, and to get specific modules for MW 1.16-) but this has not been implemented due to the extreme unlikelihood of this module ever being used outside of very specific circumstances where the user is probably doing their own parsing anyway.
			ArgumentNullException.ThrowIfNull(result);
			return result.Type == JTokenType.Array
				? new HelpResult(result.GetList<string>(), "text/html")
				: new HelpResult([result.MustHaveString("help")], result.MustHaveString("mime"));
		}
		#endregion
	}
}