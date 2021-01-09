namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using RobinHood70.CommonCode;

	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using static RobinHood70.CommonCode.Globals;

	public class LinkFinder : ParsedPageJob
	{
		#region Fields
		private readonly Title search;
		private readonly IDictionary<string, TitleCollection> results = new SortedDictionary<string, TitleCollection>(StringComparer.Ordinal);
		#endregion

		#region Constructors

		[JobInfo("Link Finder")]
		public LinkFinder(JobManager jobManager, string search)
			: base(jobManager)
		{
			ThrowNull(search, nameof(search));
			this.Pages.SetLimitations(LimitationType.None);
			this.search = Title.FromName(this.Site, search);
			this.Logger = null;
		}
		#endregion

		#region Protected Override Properties
		protected override string EditSummary => "Found links";
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			base.Main();
			Debug.WriteLine(this.results.Count);
			if (this.results.Count > 0)
			{
				this.WriteLine($"Pages linking to {this.search}");
				foreach (var result in this.results)
				{
					result.Value.Sort();
					this.WriteLine($"* {result.Key}:");
					foreach (var title in result.Value)
					{
						this.WriteLine($"** {title.AsLink(false)}");
					}

					this.WriteLine();
				}
			}
		}

		protected override void LoadPages() => this.Pages.GetBacklinks(this.search.FullPageName, BacklinksTypes.Backlinks | BacklinksTypes.ImageUsage, false, Filter.Any);

		protected override void ParseText(object sender, ContextualParser parsedPage)
		{
			ThrowNull(parsedPage, nameof(parsedPage));
			foreach (var link in parsedPage.FindLinks(this.search))
			{
				// var textTitle = link.Title.ToValue();
				var textTitle = FullTitle.FromBacklinkNode(this.Site, link).ToString();
				if (!this.results.TryGetValue(textTitle, out var entries))
				{
					entries = new TitleCollection(this.Site);
					this.results.Add(textTitle, entries);
				}

				entries.Add(parsedPage.Context);
			}
		}
		#endregion
	}
}