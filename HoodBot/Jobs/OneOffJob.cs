namespace RobinHood70.HoodBot.Jobs;

using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon;

internal sealed class OneOffJob : WikiJob
{
	[JobInfo("One-Off Job")]
	public OneOffJob(JobManager jobManager)
		: base(jobManager, JobType.Write)
	{
	}

	#region Protected Override Methods
	protected override void Main()
	{
		var rcOptions = new RecentChangesOptions()
		{
			Namespaces = [MediaWikiNamespaces.File],
			Properties = RecentChangesProperties.Ids | RecentChangesProperties.LogInfo | RecentChangesProperties.Patrolled | RecentChangesProperties.Title | RecentChangesProperties.User,
			Types = RecentChangesTypes.Log,
		};

		var changes = this.Site.LoadRecentChanges(rcOptions);
		this.ResetProgress(changes.Count);
		foreach (var change in changes)
		{
			if (change.PatrolFlags is PatrolFlags patrolFlags &&
				patrolFlags.HasFlag(PatrolFlags.Unpatrolled) &&
				(change.User?.Name.OrdinalEquals("Maintenance script") ?? false))
			{
				this.Site.Patrol(change.Id);
			}

			this.Progress++;
		}
	}
	#endregion
}