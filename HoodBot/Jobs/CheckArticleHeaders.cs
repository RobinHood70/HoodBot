namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	internal sealed class CheckArticleHeaders : WikiJob
	{
		private static readonly HashSet<string> BadHeaders = new(StringComparer.Ordinal)
		{
			"Bug",
			"Map",
			"Note",
			"Quest",
			"Quests",
			"Related Quest"
		};

		private readonly Dictionary<string, TitleCollection> exceptions = new(StringComparer.Ordinal);
		private readonly int ns;

		[JobInfo("Check Article Headers", "Maintenance|")]
		public CheckArticleHeaders(JobManager jobManager)
			: base(jobManager, JobType.ReadOnly)
		{
			this.ns = UespNamespaces.Online;
		}

		protected override void Main()
		{
			this.LoadExceptionsForNamespace();
			PageCollection pages = new(this.Site);
			pages.GetNamespace(this.ns, Filter.Exclude);
			pages.Sort();
			foreach (var page in pages)
			{
				ContextualParser parsedPage = new(page);
				foreach (var node in parsedPage.HeaderNodes)
				{
					var header = node.GetTitle(true);
					if (BadHeaders.Contains(header) && !this.IsException(page, header))
					{
						this.WriteLine($"* {SiteLink.ToText(page)} has a {header} header.");
					}
				}
			}
		}

		private void LoadExceptionsForNamespace()
		{
			switch (this.ns)
			{
				case UespNamespaces.Online:
					var titles = this.EnsureExceptionGroup("Quests");
					titles.GetCategoryMembers("Online-Places-Zones");
					titles.GetCategoryMembers("Online-Achievements-DLC Achievements");
					titles.GetCategoryMembers("Online-Achievements by Zone", true);
					break;
			}
		}

		private TitleCollection EnsureExceptionGroup(string group)
		{
			if (!this.exceptions.TryGetValue(group, out var titles))
			{
				titles = new TitleCollection(this.Site);
				this.exceptions.Add(group, titles);
			}

			return titles;
		}

		private bool IsException(Page page, string header) =>
			(string.Equals(header, "Quests", StringComparison.Ordinal) &&
			page.Title.PageName.StartsWith("Patch/", StringComparison.Ordinal))
			||
			(this.exceptions.TryGetValue(header, out var exceptionGroup) &&
			exceptionGroup.Contains(page.Title));
	}
}