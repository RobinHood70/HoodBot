#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	// MW: 1.27
	internal class ActionHelp : ActionModule<HelpInput, HelpResult>
	{
		#region Constructors
		public ActionHelp(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 0;

		public override string Name { get; } = "help";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType { get; } = RequestType.Get;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, HelpInput input)
		{
			if (this.SiteVersion < 117)
			{
				return;
			}

			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			if (this.SiteVersion < 121)
			{
				var modules = new SortedSet<string>();
				var queryModules = new SortedSet<string>();
				foreach (var module in input.Modules)
				{
					var submodule = module.Split(TextArrays.Plus, 2, StringSplitOptions.None);
					if (submodule.Length == 2 && submodule[0].Trim() == "query")
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

		protected override HelpResult DeserializeCustom(string result)
		{
			var output = new HelpResult()
			{
				Help = new List<string> { result },
				Mime = "text/html",
			};
			return output;
		}

		protected override HelpResult DeserializeResult(JToken result)
		{
			// Conceivably, results could be parsed more here (e.g., format 1.21-1.24 the same as 1.25+, and to get specific modules for MW 1.16-) but this has not been implemented due to the extreme unlikelihood of this module ever being used outside of very specific circumstances where the user is probably doing their own parsing anyway.
			ThrowNull(result, nameof(result));
			var output = new HelpResult();
			if (result.Type == JTokenType.Array)
			{
				var helpOutput = new List<string>();
				foreach (var topic in result)
				{
					helpOutput.Add((string)topic);
				}

				output.Help = helpOutput;
				output.Mime = "text/html";
			}
			else
			{
				output.Help = new List<string> { (string)result["help"] };
				output.Mime = (string)result["mime"];
			}

			return output;
		}
		#endregion
	}
}
