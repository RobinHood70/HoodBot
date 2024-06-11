namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.Text.RegularExpressions;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon;

	[method: JobInfo("One-Off Job")]
	internal sealed partial class OneOffJob(JobManager jobManager) : EditJob(jobManager)
	{
		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Create armor gallery page";

		protected override void LoadPages()
		{
			this.Pages.SetLimitations(LimitationType.None, 0);
			var groups = GetCostumeGroupings(this.Site);
			var pageText = GetGalleryText(groups);
			var page = this.Site.CreatePage("User:Alpha Kenny Buddy/Costumes", pageText);
			this.Pages.Add(page);

			groups = GetArmorGroupings(this.Site);
			pageText = GetGalleryText(groups);
			page = this.Site.CreatePage("User:Alpha Kenny Buddy/Armor", pageText);
			this.Pages.Add(page);
		}

		protected override void PageLoaded(Page page)
		{
		}
		#endregion

		#region Private Static Methods
		private static SortedDictionary<string, TitleCollection> GetArmorGroupings(Site site)
		{
			var titles = new TitleCollection(site);
			titles.GetCategoryMembers("Category:Online-Armor Images", CategoryMemberTypes.Subcat, false);
			var groups = new SortedDictionary<string, TitleCollection>(StringComparer.OrdinalIgnoreCase);
			foreach (var title in titles)
			{
				var subTitles = new TitleCollection(site);
				subTitles.GetCategoryMembers(title.FullPageName(), CategoryMemberTypes.File, false);
				var catName = title.PageName.Replace("Online-Armor Images-", string.Empty, StringComparison.Ordinal);
				groups[catName] = subTitles;
			}

			return groups;
		}

		private static SortedDictionary<string, TitleCollection> GetCostumeGroupings(Site site)
		{
			var titles = new TitleCollection(site);
			titles.GetCategoryMembers("Category:Online-Costume Images");
			var groups = new SortedDictionary<string, TitleCollection>(StringComparer.OrdinalIgnoreCase);
			var nameTrimmer = NameTrimmer();
			foreach (var title in titles)
			{
				var index = title.PageName.LastIndexOf('.');
				var name = title.PageName[..index];
				index = name.LastIndexOf('(');
				if (index != -1)
				{
					name = name[..index];
				}

				name = name.Trim();
				var match = nameTrimmer.Match(name);

				name = match.Groups["name"].Value;
				if (!groups.TryGetValue(name, out var collection))
				{
					collection = new TitleCollection(site);
					groups[name] = collection;
				}

				collection.Add(title);
			}

			return groups;
		}

		private static string GetGalleryText(SortedDictionary<string, TitleCollection> groups)
		{
			var sb = new StringBuilder();
			foreach (var (group, collection) in groups)
			{
				sb
					.Append("==")
					.Append(group)
					.AppendLine("==")
					.AppendLine("<gallery>");
				foreach (var title in collection)
				{
					sb.AppendLine(title.PageName);
				}

				sb
					.AppendLine("</gallery>")
					.AppendLine();
			}

			return sb.ToString();
		}

		[GeneratedRegex(@"ON-(costumes?|crown store|items?-armor)-(?<name>.*)$", RegexOptions.ExplicitCapture, 10000)]
		private static partial Regex NameTrimmer();
		#endregion
	}
}