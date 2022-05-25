namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;

	public class FixZenimageLicences : TemplateJob
	{
		[JobInfo("Fix Zenimage licences")]
		public FixZenimageLicences(JobManager jobManager)
			: base(jobManager)
		{
		}

		#region Protected Override Properties
		protected override string EditSummary { get; } = "Fix licencing";

		protected override string TemplateName { get; } = "Uespimage";
		#endregion

		#region Protected Override Methods
		protected override void LoadPages()
		{
			HashSet<string> prefixes = new(StringComparer.OrdinalIgnoreCase) { "card", "concept", "crown store", "icon", "load", "map", "mapicon", "prerelease", "render" };
			if (this.Site.User is null)
			{
				throw new InvalidOperationException();
			}

			/* List<long> revlist = new();
			var history = this.Site.User.GetContributions(new DateTime(2022, 02, 01), new DateTime(2022, 02, 02, 0, 3, 0));
			foreach (var item in history)
			{
				revlist.Add(item.ParentId);
			}

			this.Pages.GetRevisionIds(revlist);
			foreach (var page in this.Pages)
			{
				page.Text = page.Revisions[0].Text;
				ContextualParser parser = new(page);
				foreach (var template in parser.FindSiteTemplates("Uespimage"))
				{
					this.ParseTemplate(template, parser);
				}

				parser.UpdatePage();
			}
			*/

			TitleCollection uespImageTitles = new(this.Site);
			uespImageTitles.GetBacklinks("Template:Uespimage", BacklinksTypes.EmbeddedIn);
			TitleCollection goodTitles = new(this.Site);
			foreach (var title in uespImageTitles)
			{
				var titleSplit = title.PageName.Split('-');
				if (titleSplit.Length > 2 &&
					string.Equals(titleSplit[0], "ON", StringComparison.Ordinal) &&
					prefixes.Contains(titleSplit[1]))
				{
					goodTitles.Add(title);
				}
			}

			this.Pages.GetTitles(goodTitles);
			/*
			 * Correction for link removal in initial run due to bug
			for (var i = this.Pages.Count - 1; i >= 0; i--)
			{
				var curPage = this.Pages[i];
				if (string.Equals(curPage.Text, curPage.Revisions[0].Text, StringComparison.Ordinal))
				{
					this.Pages.RemoveAt(i);
				}
			}
			*/
		}

		protected override void ParseTemplate(SiteTemplateNode template, ContextualParser parser)
		{
			template.Title.Clear();
			template.Title.AddText("Zenimage");
			template.Parameters.Clear();
		}
		#endregion
	}
}
