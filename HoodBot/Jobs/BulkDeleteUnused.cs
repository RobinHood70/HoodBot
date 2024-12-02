namespace RobinHood70.HoodBot.Jobs;

using System;
using RobinHood70.Robby;
using RobinHood70.WikiCommon;

internal sealed class BulkDeleteUnused : WikiJob
{
	#region Fields
	private readonly TitleCollection deleteTitles;
	#endregion

	#region Constructors
	[JobInfo("Bulk Delete Unused")]
	public BulkDeleteUnused(JobManager jobManager)
		: base(jobManager, JobType.Write)
	{
		this.deleteTitles = new TitleCollection(this.Site);
	}
	#endregion

	#region Public Override Properties
	public override string LogName => "Bulk Delete";
	#endregion

	#region Protected Override Methods
	protected override bool BeforeLogging()
	{
		TitleCollection unused = new(this.Site);
		unused.GetQueryPage("Unusedimages");

		this.deleteTitles.Clear();
		foreach (var title in unused)
		{
			if (title.Namespace == MediaWikiNamespaces.File && title.PageName.StartsWith("LG-audio-", StringComparison.Ordinal))
			{
				this.deleteTitles.Add(title);
			}
		}

		return this.deleteTitles.Count > 0;
	}

	protected override void Main()
	{
		this.ProgressMaximum = this.deleteTitles.Count + 1;
		this.Progress = 1;
		foreach (var title in this.deleteTitles)
		{
			title.Delete("Unused audio file.");
			this.Progress++;
		}
	}
	#endregion
}