namespace RobinHood70.HoodBot.Jobs
{
	using System.IO;
	using RobinHood70.Robby;

	[method: JobInfo("One-Off Job")]
	internal sealed partial class OneOffJob(JobManager jobManager) : WikiJob(jobManager, JobType.ReadOnly)
	{
		#region Protected Override Methods
		protected override void Main()
		{
			var titles = new TitleCollection(this.Site);
			titles.GetCategoryMembers("Online-Furnishings", true);
			titles.Sort();
			File.WriteAllText("D:\\Online Furnishings.txt", string.Join("\r\n", titles));
		}
		#endregion
	}
}