namespace RobinHood70.HoodBot.Jobs;

using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

[method: JobInfo("One-Off Parse Job")]
public class OneOffParseJob(JobManager jobManager) : ParsedPageJob(jobManager)
{
	#region Constants
	private const string OldTemplateName = "Cleanup-oprp";
	private const string OldTemplateName2 = "Cleanup-obhrp";
	private const string TemplateName = "Cleanup-obrp-place";
	private const string TemplateText = $$$"""
		{{{{{TemplateName}}}
		|writtenBy=
		|checkedBy=
		|interior=
		|interiorChecked=
		|exterior=
		|exteriorChecked=
		}}
		""";
	#endregion

	#region Public Override Properties
	public override string LogDetails => "Add remaster place template";

	public override string LogName => "One-Off Parse Job";
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Add remaster template";

	protected override void LoadPages()
	{
		this.Shuffle = true;
		this.Pages.AllowRedirects = CommonCode.Filter.Exclude;
		// this.Pages.GetTitles("Oblivion:Roland Jenseric's Cabin");
		this.Pages.GetCategoryMembers("Oblivion-Places", CategoryMemberTypes.Page, false);
		this.Pages.GetCategoryMembers("Shivering-Places", CategoryMemberTypes.Page, false);
	}

	protected override void ParseText(SiteParser parser)
	{
		var newTemplate = TitleFactory.FromUnvalidated(this.Site[MediaWikiNamespaces.Template], TemplateName);
		if (parser.FindTemplate(newTemplate) is null)
		{
			var location = -1;
			if (OldTemplateName.Length > 0)
			{
				var oldTemplate = TitleFactory.FromValidated(this.Site[MediaWikiNamespaces.Template], OldTemplateName);
				location = parser.IndexOf<ITemplateNode>(t => t.GetTitle(this.Site) == oldTemplate);
				if (location == -1)
				{
					oldTemplate = TitleFactory.FromValidated(this.Site[MediaWikiNamespaces.Template], OldTemplateName2);
					location = parser.IndexOf<ITemplateNode>(t => t.GetTitle(this.Site) == oldTemplate);
				}
			}

			parser.InsertText(location + 1, TemplateText);
		}
	}
	#endregion
}