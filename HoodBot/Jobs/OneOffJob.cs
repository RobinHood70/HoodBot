namespace RobinHood70.HoodBot.Jobs;

using System.IO;
using RobinHood70.Robby;

[method: JobInfo("One-Off Job")]
internal sealed class OneOffJob(JobManager jobManager) : WikiJob(jobManager, JobType.ReadOnly)
{
	#region Public Override Properties
	//// public override string LogDetails => "Get category";

	public override string LogName => "One-Off Job";
	#endregion

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