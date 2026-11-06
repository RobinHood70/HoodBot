namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

internal sealed class ProtectTemplates : WikiJob
{
	#region Constants
	private const long CutoffPercentFull = 20;
	private const long CutoffPercentSemi = 2;
	private const string ProtectAutoconfirmed = "autoconfirmed";
	private const string ProtectSysop = "sysop";
	#endregion

	#region Fields
	private readonly PageCollection docPages;
	private readonly TitleDictionary<Protection> protections = new();
	#endregion

	#region Constructors
	[JobInfo("Protect Templates")]
	public ProtectTemplates(JobManager jobManager)
		: base(jobManager, JobType.Write)
	{
		this.docPages = PageCollection.Unlimited(this.Site, PageModules.Default, true);
	}
	#endregion

	#region Protected Override Methods
	protected override bool BeforeLogging()
	{
		if (!this.Site.Namespaces[MediaWikiNamespaces.Template].AllowsSubpages)
		{
			this.StatusWriteLine("Template space on this wiki doesn't allow subpages. This probably isn't a big deal, but double-check.");
		}

		var (fullCutoff, semiCutoff) = this.GetCutoffs();
		if (semiCutoff >= fullCutoff)
		{
			this.StatusWriteLine("Semi protection cutoff >= Full protection cutoff. WTF?");
			return false;
		}

		this.StatusWriteLine($"Full protection cutoff: {fullCutoff} pages or more.");
		this.StatusWriteLine($"Semi protection cutoff: {semiCutoff} pages or more.");

		this.PopulateProtections(fullCutoff, semiCutoff);
		this.CheckSubPages();
		this.UpdateProtectionLevels();
		if (this.protections.Count == 0)
		{
			return false;
		}

		this.CheckProtectionMismatches();

		var docTitles = new TitleCollection(this.Site);
		foreach (var (title, _) in this.protections)
		{
			docTitles.TryAdd(GetRootDocTitle(title));
			docTitles.TryAdd(GetThisDocTitle(title));
		}

		this.docPages.GetTitles(docTitles);
		foreach (var (title, protection) in this.protections)
		{
			// Add text before starting the save process so we get missing-docs warnings first and can decide if we want to create doc pages manually.
			var docPage = this.FindDocPage(title);
			AddTemplate(docPage, protection);
		}

		return true;
	}

	protected override void Main()
	{
		this.ResetProgress(this.protections.Count);
		foreach (var (title, protection) in this.protections)
		{
			title.Protect("High traffic page", protection.Edit, protection.Move, null);
			var docPage = this.FindDocPage(title);
			docPage.Save("Add protection template", true);
			this.Progress++;
		}
	}
	#endregion

	#region Private Static Methods
	private static void AddTemplate(Page docPage, Protection protection)
	{
		var parser = new SiteParser(docPage);
		TemplateNode? temProtect = null;
		foreach (var template in parser.TemplateNodes)
		{
			if (template.GetTitle(parser.Site).PageNameEquals("TemProtect"))
			{
				temProtect = template;
				break;
			}
		}

		if (temProtect is null)
		{
			temProtect = parser.Factory.TemplateNodeFromParts("TemProtect");
			var insertLoc = 0;
			if (parser.Count == 0 ||
				parser[insertLoc] is not TagNode tag ||
				!tag.Name.OrdinalEquals("includeonly"))
			{
				parser.Insert(insertLoc, parser.Factory.TagNode("includeonly", null, null, null));
				parser.Insert(insertLoc + 1, parser.Factory.TagNode("/includeonly", null, null, null));
				if (parser.Count > 2 && parser[insertLoc + 2] is HeaderNode)
				{
					parser.InsertText(insertLoc + 2, "\n");
				}
			}

			insertLoc++;
			parser.Insert(insertLoc, temProtect);
		}

		var parameters = temProtect.Parameters;
		if (parameters.Count > 0)
		{
			Debug.WriteLine($"Template has parameters on {docPage.Title}");
		}

		parameters.Clear();
		if (protection.Edit.OrdinalEquals(ProtectSysop) && protection.Move.OrdinalEquals(ProtectSysop))
		{
			temProtect.Add("level", "full");
		}

		parser.UpdatePage();
	}

	private static Title GetRootDocTitle(Title title) => title.WithPageName(title.RootPageName() + "/Doc");

	private static Title GetThisDocTitle(Title title) => title.SubPage("Doc");

	private void UpdateProtectionLevels()
	{
		var pageLoadOptions = new PageLoadOptions(PageModules.Info) { InfoGetProtection = true };
		var templatePages = PageCollection.Unlimited(this.Site, pageLoadOptions);
		templatePages.GetTitles(this.protections.ToTitleCollection(this.Site));

		var removeTitles = new List<Title>(this.protections.Count);
		foreach (var (title, protection) in this.protections)
		{
			var page = templatePages[title];
			var currentEditProtection = page.ProtectionLevel("edit");
			if (currentEditProtection.OrdinalEquals(ProtectSysop) || currentEditProtection.OrdinalEquals(protection.Edit))
			{
				protection.Edit = null;
			}
			else if (currentEditProtection.OrdinalEquals(ProtectSysop))
			{
				protection.Edit = currentEditProtection;
			}

			var currentMoveProtection = page.ProtectionLevel("move");
			if (currentMoveProtection.OrdinalEquals(ProtectSysop) || currentMoveProtection.OrdinalEquals(protection.Move))
			{
				protection.Move = null;
			}
			else if (currentMoveProtection.OrdinalEquals(ProtectSysop))
			{
				protection.Move = currentMoveProtection;
			}

			if (protection.Edit is null && protection.Move is null)
			{
				removeTitles.Add(title);
			}
		}

		foreach (var title in removeTitles)
		{
			this.protections.Remove(title);
		}
	}
	#endregion

	#region Private Methods
	private void CheckProtectionMismatches()
	{
		foreach (var (title, protection) in this.protections)
		{
			if (!protection.Edit.OrdinalEquals(protection.Move))
			{
				this.StatusWriteLine($"Edit and move protection levels are different for {title}: {protection.Edit} != {protection.Move}");
			}
		}
	}

	private void CheckSubPages()
	{
		foreach (var (title, _) in this.protections)
		{
			if (title.PageName.Contains('/', StringComparison.Ordinal))
			{
				var rootTitle = title.WithPageName(title.RootPageName());
				if (!this.protections.ContainsKey(rootTitle))
				{
					Debug.WriteLine($"Subpage {title} will be protected, but {rootTitle} is not.");
				}
			}
		}
	}

	private Page FindDocPage(Title title)
	{
		if (this.docPages.TryGetValue(GetThisDocTitle(title), out var docPage) && docPage.Exists)
		{
			return docPage;
		}

		var rootTitle = GetRootDocTitle(title);
		if (this.docPages.TryGetValue(rootTitle, out docPage) && docPage.Exists)
		{
			return docPage;
		}

		this.StatusWriteLine($"Documentation not found for {title}.");
		return this.Site.CreatePage(rootTitle, "==Summary==\n\n==Parameters==\n\n==Examples==\n\n<!--<includeonly>[[Category:]]</includeonly>--><noinclude>[[Category:Template Documentation]]</noinclude>");
	}

	private (long Full, long Semi) GetCutoffs()
	{
		// Cut off at pages transcluded by at least 1% of all articles, or 500 if not known.
		// Transclusion count can theoretically include more than just articles, but I can't think of any time that would matter.
		var siteInfoInput = new SiteInfoInput(SiteInfoProperties.Statistics);
		var stats = this.Site.AbstractionLayer.SiteInfo(siteInfoInput).Statistics;
		return stats is null
			? throw new InvalidOperationException("Couldn't find site statistics!")
			: (stats.Articles * CutoffPercentFull / 100, stats.Articles * CutoffPercentSemi / 100);
	}

	private void PopulateProtections(long fullCutoff, long semiCutoff)
	{
		// TODO: Rewrite ContinueModule2 (and others?) to properly ignore offsets when MaxItems has been reached; right now, LoadQueryPage is getting confused and trying to load 0 items repeatedly. This generates warnings and enumerates the entire set instead of stopping.
		// TODO: Once the above is done, rewrite querypage API to attempt to "fix" overlapping/omitted results by re-querying with an offset of half the max number of results.
		var result = this.Site.LoadQueryPage("Mostlinkedtemplates");
		foreach (var (title, data) in result)
		{
			if (title.Namespace != MediaWikiNamespaces.Template)
			{
				continue;
			}

			var dataValue = data.Value ?? throw new InvalidOperationException("Template count has no value.");
			var value = long.Parse(dataValue, this.Site.Culture);
			if (value >= semiCutoff)
			{
				var protLevel = value >= fullCutoff
					? ProtectSysop
					: ProtectAutoconfirmed;
				this.protections.Add(title, new Protection(protLevel, protLevel));
			}
		}
	}
	#endregion

	#region Private Classes
	private sealed class Protection(string? edit, string? move)
	{
		public string? Edit { get; set; } = edit;

		public string? Move { get; set; } = move;
	}
	#endregion
}