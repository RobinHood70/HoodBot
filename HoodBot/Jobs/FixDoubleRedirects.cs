namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;

	public class FixDoubleRedirects : ParsedPageJob
	{
		#region Fields
		private readonly Dictionary<Title, FullTitle> lookup = new(SimpleTitleComparer.Instance);
		#endregion

		#region Constructors
		[JobInfo("Fix Double Redirects", "Maintenance")]
		public FixDoubleRedirects(JobManager jobManager)
			: base(jobManager)
		{
			this.Pages.SetLimitations(LimitationType.None);
		}
		#endregion

		#region Protected Override Properties
		protected override Action<EditJob, Page>? EditConflictAction => null;

		protected override string EditSummary => "Fix double redirect";
		#endregion

		#region Protected Override Methods
		protected override void LoadPages()
		{
			var doubles = PageCollection.Unlimited(this.Site, PageModules.Default, true);
			doubles.GetQueryPage("DoubleRedirects");
			var titles = new TitleCollection(this.Site);
			foreach (var mapping in doubles.TitleMap)
			{
				var from = TitleFactory.FromUnvalidated(this.Site, mapping.Key);
				titles.Add(from);
				this.lookup.Add(from, mapping.Value);
			}

			this.Pages.GetTitles(titles);
		}

		protected override void ParseText(object sender, ContextualParser parser)
		{
			foreach (var link in parser.LinkNodes)
			{
				var siteLink = SiteLink.FromLinkNode(this.Site, link);
				FullTitle lookupLink = siteLink;
				while (this.lookup.TryGetValue(lookupLink, out var target))
				{
					lookupLink = target;
				}

				var newLink = new SiteLink((IFullTitle)lookupLink);
				if (siteLink.Fragment is not null)
				{
					newLink.Fragment = siteLink.Fragment;
				}

				newLink.UpdateLinkNode(link);
			}
		}
		#endregion
	}
}