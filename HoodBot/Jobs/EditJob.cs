namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.Tasks;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;

	public abstract class EditJob : WikiJob
	{
		protected EditJob(Site site, AsyncInfo asyncInfo, params WikiTask[] tasks)
			: base(site, asyncInfo, tasks) => this.ReadOnly = false;

		/* In retrospect, these may never be used.

		public event StrongEventHandler<EditJob, Page> Saving;

		public event StrongEventHandler<EditJob, Page> Saved;
		*/
		#region Public Virtual Properties
		public virtual string LogDetails { get; protected set; }
		#endregion

		#region Public Abstract Properties
		public abstract string LogName { get; }
		#endregion

		#region Protected Override Methods
		protected override void OnCompleted()
		{
			this.Site.UserFunctions.EndLogEntry();
			base.OnCompleted();
		}

		protected override void OnStarted()
		{
			base.OnStarted();
			this.StatusWriteLine("Adding Log Entry");
			this.Site.UserFunctions.AddLogEntry(new LogInfo(this.LogName ?? this.GetType().Name, this.LogDetails, this.ReadOnly));
		}
		#endregion
	}
}