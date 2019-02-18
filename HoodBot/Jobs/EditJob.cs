namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.Tasks;
	using RobinHood70.Robby;

	public class EditJob : WikiJob
	{
		public EditJob(Site site, AsyncInfo asyncInfo, params WikiTask[] tasks)
			: base(site, asyncInfo, tasks)
		{
		}

		protected override void MainJob() => throw new System.NotImplementedException();
	}
}
