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
	private const string TemplateText = """
		{{Cleanup-obrp-npc
		|image=
		|imageChecked=
		|dialogue=
		|dialogueChecked=
		|quests=
		|questsChecked=
		|services=
		|servicesChecked=
		|inventory=
		|inventoryChecked=
		|house=
		|houseChecked=
		|schedule=
		|scheduleChecked=
		}}
		""";
	#endregion

	#region Public Override Properties
	public override string LogDetails => "Add remaster NPC template";

	public override string LogName => "One-Off Parse Job";
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Add remaster template";

	protected override void LoadPages()
	{
		this.Shuffle = true;
		this.Pages.AllowRedirects = CommonCode.Filter.Exclude;
		this.Pages.GetCategoryMembers("Oblivion-NPCs", CategoryMemberTypes.Page, false);
		this.Pages.GetCategoryMembers("Shivering-NPCs", CategoryMemberTypes.Page, false);
	}

	protected override void ParseText(SiteParser parser)
	{
		var newTemplate = TitleFactory.FromUnvalidated(this.Site[MediaWikiNamespaces.Template], "Cleanup-obrp-npc");
		if (parser.FindTemplate(newTemplate) is null)
		{
			var oldTemplate = TitleFactory.FromValidated(this.Site[MediaWikiNamespaces.Template], "Cleanup-obnpcrp");
			var location = parser.IndexOf<ITemplateNode>(t => t.GetTitle(this.Site) == oldTemplate);
			parser.InsertText(location + 1, TemplateText);
		}
	}
	#endregion
}