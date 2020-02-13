namespace RobinHood70.HoodBot.Jobs
{
	using System.Diagnostics;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;
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
			var siteLinkNode = SiteLink.FromText(this.Site, "[[ File:Example.jpg | thumb | left | link=Oblivion:Oblivion | test=Quick test]]");
			siteLinkNode.AltText = "Test";
			siteLinkNode.Border = true;
			Debug.WriteLine(siteLinkNode.Link);
			Debug.WriteLine(siteLinkNode.AltText);
			Debug.WriteLine(siteLinkNode.Text);
			Debug.WriteLine(siteLinkNode.ToString());
		}
		#endregion
	}
}
