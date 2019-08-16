namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon;

	public class OneOffJob : EditJob
	{
		#region Constructors
		[JobInfo("One-Off Job")]
		public OneOffJob([ValidatedNotNull] Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
		}
		#endregion

		#region Public Override Properties
		public override string LogName => "Add deck codes";
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			/*
			foreach (var page in this.pages)
			{
				this.SavePage(page, "Add deck code", true);
				this.Progress++;
			}
			*/
		}

		protected override void PrepareJob()
		{
		}
		#endregion
	}
}
