namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon.Parser;

[method: JobInfo("One-Off Parse Job")]
public class OneOffParseJob(JobManager jobManager) : ParsedPageJob(jobManager)
{
	#region Static Fields
	private static readonly Regex Header = new(@"{\|\s*class=wikitable\s*\n!\s*Planet\s*!!\s*Biomes\s*!!\s*Resource(\s*!!\s*(Outpost )?Production( Allowed)?)? *(?<tabletext>(?s:.+?))\n\|}", RegexOptions.IgnoreCase, Globals.DefaultRegexTimeout);
	#endregion

	#region Public Override Properties
	public override string LogDetails => "Replace table flora variants with templates";

	public override string LogName => "One-Off Parse Job";
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Replace table with templates";

	protected override void LoadPages() => this.Pages.GetCategoryMembers("Starfield-Flora");

	protected override void ParseText(SiteParser parser)
	{
		this.Shuffle = true;
		if (parser.FindTemplate("Flora Variant Table") is not null)
		{
			return;
		}

		var sections = parser.ToSections(2);
		var found = false;
		foreach (var section in sections)
		{
			if (string.Equals(section.Header.GetTitle(true), "Variants", System.StringComparison.Ordinal))
			{
				found = true;
				var text = section.Content.ToRaw();
				if (!Header.Match(text).Success)
				{
					Debug.WriteLine("Table not found on [[" + parser.Title + "]]");
				}

				var newText = Header.Replace(text, this.Replacer);
				section.Content.Clear();
				section.Content.AddText(newText);
			}
		}

		if (!found)
		{
			Debug.WriteLine("No variants section on [[" + parser.Title + "]]");
		}

		parser.FromSections(sections);
	}

	private string Replacer(Match match)
	{
		var sb = new StringBuilder(match.Value.Length);
		sb.Append("{{Flora Variant Table|\n");
		var rows = match.Groups["tabletext"].Value.Split("\n|-");
		foreach (var row in rows)
		{
			if (row.Length == 0)
			{
				continue;
			}

			var rowText = row.Trim().TrimStart(TextArrays.Pipe);
			var cells = new List<string>(rowText.Split("||", StringSplitOptions.TrimEntries));
			if (cells.Count == 3 && cells[2].EndsWith("]]", StringComparison.Ordinal))
			{
				cells.Add(string.Empty);
			}

			if (cells.Count != 4)
			{
				throw new InvalidOperationException();
			}

			var planetLink = SiteLink.FromText(this.Site, cells[0]);
			var resourceLink = SiteLink.FromText(this.Site, cells[2]);
			sb
				.Append("  {{Flora Variant")
				.Append("|planet=")
				.Append(planetLink.Title.PageName)
				.Append("|biomes=")
				.Append(cells[1])
				.Append("|resource=")
				.Append(resourceLink.Title.PageName)
				.Append("|productionallowed=")
				.Append(cells[3])
				.Append("}}\n");
		}

		sb.Append("}}");
		return sb.ToString();
	}
	#endregion
}