namespace RobinHood70.HoodBot.Jobs;

using System;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon;

internal sealed class OneOffJob : WikiJob
{
	#region Fields
	private readonly TitleCollection toDelete;
	#endregion

	#region Constructors
	[JobInfo("One-Off Job")]
	public OneOffJob(JobManager jobManager)
		: base(jobManager, JobType.Write)
	{
		this.toDelete = new TitleCollection(this.Site);
	}
	#endregion

	#region Protected Override Methods
	protected override bool BeforeLogging()
	{
		var titles = this.GetDeletionLog();
		var pages = new PageCollection(this.Site, PageModules.Info | PageModules.Backlinks);
		pages.GetTitles(titles);
		foreach (var page in pages)
		{
			if (page.IsRedirect && page.Backlinks.Count == 0)
			{
				this.toDelete.Add(page.Title);
			}
		}

		return this.toDelete.Count > 0;
	}

	protected override void Main()
	{
		this.ResetProgress(this.toDelete.Count);
		foreach (var title in this.toDelete)
		{
			title.Delete("Unnecessary redirect");
			this.Progress++;
		}
	}
	#endregion

	#region Private Methods
	private TitleCollection GetDeletionLog()
	{
		var retval = new TitleCollection(this.Site);
		var input = LogEventsInput.FromNamespace(MediaWikiNamespaces.File);
		input.Start = new DateTime(2026, 9, 13);
		input.SortDescending = true;
		input.Properties = LogEventsProperties.Details | LogEventsProperties.Title | LogEventsProperties.User | LogEventsProperties.Timestamp;
		input.Type = "move";
		foreach (var logEvent in this.Site.AbstractionLayer.LogEvents(input))
		{
			if (logEvent.User.OrdinalEquals("KevinM") &&
				logEvent.ExtraData?.TryGetValue("target_title", out var sourceTitleObj) == true &&
				sourceTitleObj is string sourceTitle)
			{
				retval.Add(TitleFactory.FromValidated(this.Site, sourceTitle));
			}
		}

		return retval;
	}
	#endregion
}