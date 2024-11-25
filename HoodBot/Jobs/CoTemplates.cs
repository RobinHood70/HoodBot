﻿namespace RobinHood70.HoodBot.Jobs;

using System.Diagnostics;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;

internal sealed class CoTemplates : WikiJob
{
	#region Constructors
	[JobInfo("Co-occurring Templates")]
	public CoTemplates(JobManager jobManager, string template1, string template2)
		: base(jobManager, JobType.ReadOnly)
	{
		this.Title1 = TitleFactory.FromTemplate(this.Site, template1);
		this.Title2 = TitleFactory.FromTemplate(this.Site, template2);
	}
	#endregion

	#region Public Properties
	public Title Title1 { get; }

	public Title Title2 { get; }
	#endregion

	#region Protected Override Methods
	protected override void Main()
	{
		var pages = PageCollection.Unlimited(this.Site, PageModules.Templates, true);
		pages.GetBacklinks(this.Title1.FullPageName());
		foreach (var page in pages)
		{
			var titles = new TitleCollection(this.Site, page.Templates);
			if (titles.Contains(this.Title2))
			{
				Debug.WriteLine($"* {SiteLink.ToText(page.Title, LinkFormat.LabelName)}");
			}
		}
	}
	#endregion
}