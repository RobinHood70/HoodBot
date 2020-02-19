namespace RobinHood70.HoodBot.Jobs
{
	using System.Diagnostics;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
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
			var tp1 = new TitleParts(this.Site, ":_Oblivion_:_Oblivion_#_Quest\xA0Information_");
			Debug.WriteLine($"{tp1.OriginalInterwikiText} => {tp1.Interwiki?.Prefix}");
			Debug.WriteLine($"{tp1.OriginalNamespaceText} => {tp1.Namespace.Name}");
			Debug.WriteLine($"{tp1.OriginalPageNameText} => {tp1.PageName}");
			Debug.WriteLine($"{tp1.OriginalFragmentText} => {tp1.Fragment}");
		}
		#endregion
	}
}
