namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Windows;
	using RobinHood70.CommonCode;

	using RobinHood70.Robby;
	using RobinHood70.WallE.Design;

	public class BlockChanger : EditJob
	{
		#region Private Constants
		private const int NumYears = 1;
		#endregion

		#region Fields
		private readonly List<Block> reblocks = new();
		#endregion

		#region Constructors
		[JobInfo("Fix Infinite IP Blocks", "Maintenance")]
		public BlockChanger(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Public Override Properties
		public override string LogName => "Fix Infinite IP Blocks";
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			foreach (var block in this.reblocks)
			{
				try
				{
					if (block.User is string userName)
					{
						var user = new User(User.GetTitle(this.Site, block.User));
						if (block.StartTime <= DateTime.Now.AddYears(-NumYears))
						{
							user.Unblock("Remove infinite IP block");
						}
						else
						{
							user.Block("Re-block with finite block length", block.Flags, block.StartTime.AddYears(NumYears), reblock: true);
						}
					}
				}
				catch (WikiException ex)
				{
					MessageBox.Show(ex.Info, ex.Code);
				}

				this.Progress++;
			}
		}

		protected override void BeforeLogging()
		{
			var comparer = this.Site.Culture.CompareInfo;
			var blocks = this.Site.LoadBlocks(Filter.Exclude, Filter.Any, Filter.Exclude, Filter.Exclude);
			foreach (var block in blocks)
			{
				if (block.Reason == null || (comparer.IndexOf(block.Reason, "proxy", CompareOptions.IgnoreCase) == -1 && comparer.IndexOf(block.Reason, "tor", CompareOptions.IgnoreCase) == -1))
				{
					this.reblocks.Add(block);
				}
			}

			this.ProgressMaximum = this.reblocks.Count;
		}
		#endregion
	}
}