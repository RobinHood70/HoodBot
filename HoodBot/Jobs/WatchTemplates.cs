namespace RobinHood70.HoodBot.Jobs
{
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon;

	public class WatchTemplates : WikiJob
	{
		[JobInfo("Watch Template Space")]
		public WatchTemplates([NotNull, ValidatedNotNull] Site site, [NotNull, ValidatedNotNull] AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
		}

		protected override void Main()
		{
			var pages = this.Site.Watch(MediaWikiNamespaces.Template);
			foreach (var page in pages.Value)
			{
				Debug.WriteLine(page.PageName);
			}
		}
	}
}
