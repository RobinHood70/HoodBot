namespace RobinHood70.HoodBot.Jobs;

using System.Collections.Generic;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.WallE.Base;

[method: JobInfo("One-Off Job")]
internal sealed class OneOffJob(JobManager jobManager) : WikiJob(jobManager, JobType.Write)
{
	#region Public Override Properties
	public override string LogName => "One-Off Job";
	#endregion

	#region Protected Override Methods
	protected override void Main()
	{
		// Filter list first for better ETA
		var cloudFlareBlocks = new List<Block>();
		foreach (var block in this.Site.LoadBlocks(Filter.Exclude, Filter.Any, Filter.Any, Filter.Any))
		{
			if (block.User is not null &&
				block.Reason is string reason &&
				reason.Contains("cloudflare", System.StringComparison.OrdinalIgnoreCase))
			{
				cloudFlareBlocks.Add(block);
			}
		}

		this.ProgressMaximum = cloudFlareBlocks.Count;
		foreach (var block in cloudFlareBlocks)
		{
			var input = new UnblockInput(block.User!.Name)
			{
				Reason = "Unblock Cloudflare"
			};

			this.Site.AbstractionLayer.Unblock(input);
			this.Progress++;
		}
	}
	#endregion

}