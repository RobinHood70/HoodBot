namespace RobinHood70.HoodBot.Jobs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Uesp;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon.Parser;

internal sealed partial class UpdateFromPeoplePage : ParsedPageJob
{
	#region Private Constants
	private const string CityName = "Narsis";
	private const string CityType = "city";
	#endregion

	#region Fields
	private readonly Dictionary<Title, List<string>> extraCells = [];
	private readonly HashSet<string> cityValues = new(StringComparer.OrdinalIgnoreCase);
	private readonly List<string> warnings = [];
	private readonly Context context;
	#endregion

	#region Constructors
	[JobInfo("Update NPC from People page")]
	public UpdateFromPeoplePage(JobManager jobManager)
		: base(jobManager)
	{
		this.cityValues.Add(CityName);
		this.cityValues.Add($"[[TR3:{CityName}|{CityName}]]");
		this.cityValues.Add($"[[Tamriel Rebuilt:{CityName}|{CityName}]]");
		this.cityValues.Add("{{TR3|" + CityName + "}}");
		this.context = new Context(this.Site);
	}
	#endregion

	#region Protected Override Methods
	protected override void AfterLoadPages()
	{
		if (false && this.warnings.Count > 0)
		{
			this.warnings.Sort(StringComparer.Ordinal);
			this.WriteLine("{| class=\"wikitable\"");
			this.WriteLine("! Page !! Parameter<br>Name !! Previous<br>Value !! New<br>Value");
			foreach (var warning in this.warnings)
			{
				this.WriteLine("|-");
				this.WriteLine(warning);
			}

			this.WriteLine("|}");
			this.Results!.Save();
		}
	}

	protected override string GetEditSummary(Page page) => "Update data from People page";

	protected override void LoadPages()
	{
		var sourceTitle = TitleFactory.FromUnvalidated(this.Site, "Tamriel Rebuilt:People in " + CityName);
		var pseudoNs = new UespNamespaceList(this.Site).FromTitle(sourceTitle) ?? throw new InvalidOperationException();
		var full = pseudoNs.Full;
		var sourcePage = this.Site.LoadPage(sourceTitle);
		var parser = new SiteParser(sourcePage);
		var lastNode = parser.Count;
		var npcTitles = new TitleCollection(this.Site);
		for (var i = parser.Count - 1; i >= 0; i--)
		{
			if (parser[i] is ITemplateNode t && t.GetTitle(this.Site) == "Template:NPC Data")
			{
				var name = t.Find(1)?.Value.ToRaw() ?? throw new InvalidOperationException("NPC Data template missing name.");
				var title = TitleFactory.FromUnvalidated(this.Site, full + name);
				npcTitles.Add(title);
				var nodes = new WikiNodeCollection(parser.Factory, parser[(i + 1)..lastNode]);
				lastNode = i;
				var cells = GetCellsFromNodes(nodes);
				this.extraCells.Add(title, cells);
			}
		}

		this.Pages.GetTitles(npcTitles);
	}

	protected override void ParseText(SiteParser parser)
	{
		var isSummary = true;
		if (parser.FindTemplate("NPC Summary") is not ITemplateNode template)
		{
			isSummary = false;
			template = parser.FindTemplate("Non-Relevant NPC")!;
			if (template is null)
			{
				Debug.WriteLine("Template not found on " + parser.Title);
				return;
			}
		}

		var city = isSummary ? $"[[TR3:{CityName}|{CityName}]]" : CityName;
		var cityParam = template.UpdateIfEmpty(CityType, city);
		var cityText = cityParam.GetValue();
		if (!this.cityValues.Contains(cityText))
		{
			this.warnings.Add($"| {parser.Title} || city || {cityText} || {city}");
		}

		var cells = this.extraCells[parser.Title];
		if (cells.Count > 0)
		{
			var loc = cells[0].Trim();
			if (loc.Length > 0)
			{
				if (template.Find("loc")?.Value is WikiNodeCollection locText)
				{
					var locPlain = ParseToText.Build(loc, this.context);
					var locTextPlain = ParseToText.Build(locText, this.context).Trim();
					locTextPlain = locTextPlain.Replace(CityName + ", ", string.Empty, StringComparison.OrdinalIgnoreCase);

					if (!string.IsNullOrWhiteSpace(locTextPlain) && !locTextPlain.OrdinalICEquals(locPlain))
					{
						this.warnings.Add($"| {parser.Title} || loc || {locText.ToRaw().Trim()} || {loc}");
					}
				}

				template.Update("loc", loc, ParameterFormat.OnePerLine, true);
			}

			if (cells.Count > 1)
			{
				var notes = cells[1].Trim();
				if (notes.Length > 0)
				{
					template.UpdateIfEmpty("notes", notes, ParameterFormat.OnePerLine);
				}
			}
		}
	}
	#endregion

	#region Private Methods
	private static List<string> GetCellsFromNodes(WikiNodeCollection nodes)
	{
		var split = nodes.Split(CellMarkers(), false);
		if (split.Count > 0)
		{
			var text = split[0].ToRaw().Trim();
			if (text.Length != 0)
			{
				throw new InvalidOperationException("First node not null: " + text);
			}
		}

		var retval = new List<string>();
		for (var i = 1; i < split.Count; i++)
		{
			var text = split[i].ToRaw();
			if (text.Length > 0 && (text[0] == '}' || text[0] == '-'))
			{
				// If we've hit a row break or the end of the table, stop.
				break;
			}

			retval.Add(split[i].ToRaw());
		}

		return retval;

		// TODO: For now, this is just a quick text conversion, then check for ||. For accuracy, should be converted to scan only ITextNodes and check for either || or \n|, taking all text/nodes in between as part of the cell.
		/*
		var text = nodes.ToRaw();
		var tableClose = text.IndexOf("\n|}", StringComparison.Ordinal);
		if (tableClose != -1)
		{
			text = text[..tableClose];
		}

		var split = text.Split("||", StringSplitOptions.TrimEntries);
		return [.. split[1..]];
		*/
	}

	[GeneratedRegex(@"(\|\||\n\|)", RegexOptions.None, Globals.DefaultGeneratedRegexTimeout)]
	private static partial Regex CellMarkers();
	#endregion
}