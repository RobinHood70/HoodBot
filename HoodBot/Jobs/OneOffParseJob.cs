namespace RobinHood70.HoodBot.Jobs;

using System;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

internal sealed class OneOffParseJob : ParsedPageJob
{
	#region Private Constants
	private const string TemplateName = "Cleanup-obrp-place";
	#endregion

	#region Static Fields
	private TitleCollection TemplateNames;
	#endregion

	#region Constructors
	[JobInfo("One-Off Parse Job")]
	public OneOffParseJob(JobManager jobManager)
		: base(jobManager)
	{
		this.TemplateNames = new TitleCollection(this.Site, MediaWikiNamespaces.Template, TemplateName, "Cleanup-obhrp", "Cleanup-oprp", "Description", "Featured Article", "Incomplete", "Trail");
	}
	#endregion

	#region Public Override Properties
	public override string LogDetails => "Fix template vs noinclude";
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => this.LogDetails;

	protected override void LoadPages()
	{
		this.Pages.GetBacklinks("Template:" + TemplateName, BacklinksTypes.EmbeddedIn);
		this.Pages.GetBacklinks("Template:Cleanup-oprp", BacklinksTypes.EmbeddedIn);
	}

	protected override void PageLoaded(Page page)
	{
		SiteParser parser = new(page, InclusionType.Transcluded, false);
		this.ParseText(parser);
		parser.UpdatePage();
		parser.Page.Text = parser.Page.Text
			.Replace("\r\n", "\n", StringComparison.Ordinal)
			.Replace("</noinclude>__NOTOC__", "__NOTOC__</noinclude>", StringComparison.OrdinalIgnoreCase);
	}

	protected override void ParseText(SiteParser parser)
	{
		var start = parser.FindIndex(
			t => t is ITemplateNode template &&
			TitleFactory.FromTemplate(this.Site, template.TitleNodes.ToValue()).PageNameEquals(TemplateName));
		if (start == -1)
		{
			return;
		}

		var end = start + 1;

		while (start > 0 && (
			(parser[start - 1] is ITemplateNode prev && this.TemplateNames.Contains(TitleFactory.FromTemplate(this.Site, prev.TitleNodes.ToValue()))) ||
			(parser[start - 1] is ITextNode text && string.IsNullOrWhiteSpace(text.Text))))
		{
			--start;
		}

		while (end < (parser.Count - 1) && (
			(parser[end] is ITemplateNode next && this.TemplateNames.Contains(TitleFactory.FromTemplate(this.Site, next.TitleNodes.ToValue()))) ||
			(parser[end] is ITextNode text && string.IsNullOrWhiteSpace(text.Text))))
		{
			++end;
		}

		// Using text insert below because it's a lot easier than re-parsing the template (and possible successive ignored stuff) into an ignore node.
		if (parser[end] is IIgnoreNode node && node.Value.StartsWith("<noinclude>", System.StringComparison.OrdinalIgnoreCase))
		{
			var value = node.Value[11..];
			parser.RemoveAt(end);
			parser.Insert(end, parser.Factory.IgnoreNode(value));
		}
		else
		{
			parser.InsertText(end, "</noinclude>");
		}

		parser.InsertText(start, "<noinclude>");
	}
	#endregion
}