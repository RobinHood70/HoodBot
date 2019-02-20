namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.Tasks;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon;

	public abstract class EditJob : WikiJob
	{
		protected EditJob(Site site, AsyncInfo asyncInfo, params WikiTask[] tasks)
			: base(site, asyncInfo, tasks)
		{
		}

		public event StrongEventHandler<EditJob, Page> Saving;

		public event StrongEventHandler<EditJob, Page> Saved;

		protected override void OnCompleted()
		{
			this.Site.UserFunctions.EndLogEntry();
			base.OnCompleted();
		}

		protected override void OnStarted()
		{
			base.OnStarted();
			this.Site.UserFunctions.BeginLogEntry();
		}
	}
}