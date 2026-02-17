namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Diagnostics;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Uesp;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

internal sealed class OneOffParseJob : ParsedPageJob
{
	#region Fields
	private readonly PageCollection npcs;
	private readonly TitleCollection delete;
	#endregion

	#region Constructors
	[JobInfo("One-Off Parse Job")]
	public OneOffParseJob(JobManager jobManager)
		: base(jobManager)
	{
		this.delete = new TitleCollection(this.Site);
		this.npcs = new PageCollection(this.Site);
	}
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Add NPC Summary";

	protected override void LoadPages()
	{
		this.npcs.GetBacklinks("Template:NPC Summary", BacklinksTypes.EmbeddedIn, false, Filter.Exclude, UespNamespaces.Daggerfall);
		this.Pages.GetCategoryMembers("Daggerfall-People");
	}

	protected override void Main()
	{
		base.Main();
		foreach (var title in this.delete)
		{
			title.Delete("Information copied to main NPC page");
		}
	}

	protected override void ParseText(SiteParser parser)
	{
		foreach (var template in parser.TemplateNodes)
		{
			if (template.GetTitleText().Contains("Summary", StringComparison.Ordinal))
			{
				Debug.WriteLine($"{parser.Title} already has a summary, skipping.");
				return;
			}
		}

		var sections = parser.ToSections();
		var lead = sections[0].Content;
		if (this.FindMainImage(lead) is not SiteLink mainImage)
		{
			Debug.WriteLine($"Could not find main image for {parser.Title}");
			return;
		}

		this.RemovePeopleTrail(lead);
		lead.TrimStart();

		var npcSummaryText = this.GetNpcSummary(parser);
		lead.InsertParsed(0, npcSummaryText);
		this.UpdateNpcSummary(lead, mainImage);

		parser.FromSections(sections);
	}
	#endregion

	#region Private Methods
	private void UpdateNpcSummary(WikiNodeCollection lead, SiteLink mainImage)
	{
		if (lead.FindTemplate(this.Site, "NPC Summary") is not ITemplateNode npcSummary)
		{
			throw new InvalidOperationException("Could not find NPC Summary. WTF?");
		}

		npcSummary.Update("image", mainImage.Title.PageName, ParameterFormat.OnePerLine, false);
		npcSummary.Update("imgdesc", mainImage.Text, ParameterFormat.OnePerLine, false);
	}

	private string GetNpcSummary(SiteParser parser)
	{
		if (this.npcs.TryGetValue(parser.Title + " (NPC)", out var npcPage))
		{
			// Note: this was bugged in the original version but fixed before committed to GitHub. The original version was adding the main page title to the delete list instead of the NPC page title.
			this.delete.Add(npcPage.Title);
			return npcPage.Text.Trim() + '\n';
		}

		return """
		{{NPC Summary
		|id=
		|lorelink=
		|city=
		|loc=
		|race=
		|region=
		|gender=
		|type=
		|faction=
		|sgroup=
		|ggroup=
		|parentfaction=
		|ally1=
		|ally2=
		|enemy1=
		|enemy2=
		|enemy3=
		|power=
		|minf=
		|maxf=
		|summon=
		|image=
		|imgdesc=
		}}

		""";
	}

	private void RemovePeopleTrail(WikiNodeCollection lead)
	{
		if (lead.FindTemplate(this.Site, "Trail") is ITemplateNode trail &&
			trail.Find(1) is IParameterNode parameter &&
			parameter.GetValue().OrdinalEquals("People"))
		{
			lead.Remove(trail);
		}
	}

	private SiteLink? FindMainImage(WikiNodeCollection lead)
	{
		foreach (var link in lead.LinkNodes)
		{
			var siteLink = SiteLink.FromLinkNode(this.Site, link);
			if (siteLink.HorizontalAlignment.OrdinalEquals("left"))
			{
				lead.Remove(link);
				return siteLink;
			}
		}

		return null;
	}
	#endregion
}