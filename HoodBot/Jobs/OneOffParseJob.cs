namespace RobinHood70.HoodBot.Jobs;

using System;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;

internal sealed class OneOffParseJob : ParsedPageJob
{
	#region Constants
	private const string BaseWiki = "en";
	private const string OtherWiki = "fr";
	#endregion

	#region Fields
	private readonly string category = BaseWiki switch
	{
		"en" => "Morrowind-Books",
		"fr" => "Morrowind-Livres",
		_ => throw new NotSupportedException()
	};

	private readonly string editSummary = BaseWiki switch
	{
		"en" => "Add interlanguage link",
		"fr" => "Ajouter un lien interlangue",
		_ => throw new NotSupportedException()
	};
	#endregion

	#region Constructors
	public OneOffParseJob(JobManager jobManager)
		: base(jobManager)
	{
		if (jobManager.Site.AbstractionLayer == null)
		{
		}
	}
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => this.editSummary;

	protected override void LoadPages() => this.Pages.GetCategoryMembers(this.category, false);

	protected override void ParseText(SiteParser parser)
	{
		return;
	}
	#endregion
}