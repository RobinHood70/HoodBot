namespace RobinHood70.HoodBot.Jobs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

[method: JobInfo("Health Replacer", "ESO")]
public class EsoHealthReplacer(JobManager jobManager) : ParsedPageJob(jobManager)
{
	#region Static Fields
	private static readonly Dictionary<string, string> EsoHealthReplacements = new(StringComparer.InvariantCultureIgnoreCase)
	{
		["1"] = "w",
		["2"] = "ba",
		["3"] = "a",
		["4"] = "aa",
		["5"] = "s",
		["13187"] = "vw",
		["13846"] = "w",
		["15000"] = "ncf",
		["18840"] = "cyrw",
		["29870"] = "ba",
		["31364"] = "a",
		["34501"] = "aa",
		["40643"] = "cyrba",
		["42677"] = "cyr",
		["46945"] = "cyraa",
		["54372"] = "cyrfa",
		["57497"] = "sl",
		["59939"] = "jnh",
		["60370"] = "s",
		["66121"] = "sh",
		["66924"] = "dbt",
		["82145"] = "cyrs",
		["103494"] = "el",
		["108669"] = "em",
		["119538"] = "eh",
		["127470"] = "dbl",
		["133844"] = "dba",
		["140824"] = "cyrel",
		["146590"] = "dbh",
		["147865"] = "cyrem",
		["162654"] = "cyreh",
		["cyrfh"] = "cyrfa",
		["eb"] = "dba",
		["fh"] = "fa",
		["h"] = "aa",
		["high"] = "aa",
		["jh"] = "jna",
		["jl"] = "jnl",
		["jvh"] = "jnh",
		["l"] = "ba",
		["low"] = "ba",
		["n"] = "a",
		["normal"] = "a",
	};
	#endregion

	#region Public Override Properties
	public override string LogDetails => "Replace health values with ESO Health";

	public override string LogName => "ESO Health Replacer";
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Replace values with templates";

	protected override void LoadPages() => this.Pages.GetBacklinks("Template:Online NPC Summary", BacklinksTypes.EmbeddedIn, true, Filter.Exclude);

	protected override void ParseText(SiteParser parser)
	{
		foreach (var template in parser.FindTemplates("ESO Health"))
		{
			if (template.Find(1) is IParameterNode param &&
				param.Value is WikiNodeCollection nodes &&
				EsoHealthReplacements.TryGetValue(nodes.ToRaw().Trim().Replace(",", string.Empty, StringComparison.Ordinal), out var newValue))
			{
				param.SetValue(newValue, ParameterFormat.Copy);
			}
		}

		string? globalReaction = null;
		foreach (var npcTemplate in parser.FindTemplates("Online NPC Summary"))
		{
			var reaction = npcTemplate.Find("reaction")?.GetValue() ?? string.Empty;
			if (globalReaction is null)
			{
				globalReaction = reaction;
			}
			else if (!string.Equals(globalReaction, reaction, StringComparison.Ordinal))
			{
				globalReaction = string.Empty;
			}

			if (npcTemplate.Find("health") is IParameterNode healthParam &&
				healthParam.Value is WikiNodeCollection healthNodes)
			{
				foreach (var entry in EsoHealthReplacements)
				{
					if (entry.Key.Length > 4 && int.TryParse(entry.Key, CultureInfo.CurrentCulture, out var _))
					{
						var regexText = entry.Key[..^3] + ",?" + entry.Key[^3..];
						healthParam.Value.RegexReplace($@"\b{regexText}\b", $"{{{{ESO Health|{entry.Value}}}}}", RegexOptions.None, ReplaceLocations.Text);
					}
				}
			}
		}

		foreach (var entry in EsoHealthReplacements)
		{
			if (entry.Key.Length > 4 && int.TryParse(entry.Key, CultureInfo.CurrentCulture, out var _))
			{
				var regexText = entry.Key[..^3] + ",?" + entry.Key[^3..];
				/*
				if (entry.Value[0] == 'j' && globalReaction?.Length > 0 && Regex.Match(parser.ToRaw(), regexText, RegexOptions.None, Globals.DefaultRegexTimeout).Success)
				{
					Debug.WriteLine(parser.Page.Title + ": " + globalReaction);
				}
				*/

				parser.RegexReplace($@"\b{regexText}\b", $"{{{{ESO Health|{entry.Value}}}}}", RegexOptions.None, ReplaceLocations.Text);
			}
		}
	}
	#endregion
}