﻿namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;
    using RobinHood70.WallE.Eve;
    using RobinHood70.WikiCommon;

	public class TestJob : WikiJob
	{
		#region Constructors
		[JobInfo("Test Job")]
		public TestJob([ValidatedNotNull] Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
		}
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			var wal = this.Site.AbstractionLayer as WikiAbstractionLayer;
			wal.MaxLag = -1;
			var test = this.Site.LoadUserInformation("RobinHood70");
		}
		#endregion
	}
}
