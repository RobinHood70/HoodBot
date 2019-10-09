namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using System.Text.RegularExpressions;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiClasses;
	using RobinHood70.WikiCommon;

	public class OneOffJob : EditJob
	{
		#region Constructors
		[JobInfo("One-Off Job")]
		public OneOffJob([ValidatedNotNull] Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
		}

		public override string LogName => "Update Lore Transclusions Page";
		#endregion

		#region Protected Override Methods
		protected override void PrepareJob()
		{
			this.Site.UserFunctions.ResultsPage = new Page(this.Site, this.Site.User.FullPageName + "/Lore Transclusions");
			this.Site.UserFunctions.SetResultTitle(ResultDestination.ResultsPage, "Lore Transclusions (minus all Game Book pages, even if they have a Manual Lore transclusion)");
			var allTransclusions = GetLoreTransclusions(this.Site);

			Namespace lastNamespace = null;
			foreach (var page in allTransclusions)
			{
				var links = FindLoreTransclusions(page.Text);
				if (links.Count > 0)
				{
					if (page.Namespace != lastNamespace)
					{
						lastNamespace = page.Namespace;
						this.WriteLine();
						this.WriteLine($"== {lastNamespace.Name} ==");
					}

					this.WriteLine($"* [[{page.FullPageName}]] transcludes: {string.Join(", ", links)}");
				}
			}
		}

		protected override void Main()
		{
		}
		#endregion

		#region Private Static Methods
		private static List<string> FindLoreTransclusions(string text)
		{
			var search = Template.FindRaw(null, @"(?i:l)ore:[^#\|}]+?", null, RegexOptions.None, 10);
			var matches = search.Matches(text);
			var links = new List<string>(matches.Count);
			foreach (Match match in matches)
			{
				var template = Template.Parse(match.Value);
				links.Add($"[[{template.Name}|]]");
			}

			return links;
		}

		private static PageCollection GetLoreTransclusions(Site site)
		{
			var allTransclusions = PageCollection.Unlimited(site, new PageLoadOptions(PageModules.Info | PageModules.Revisions));
			allTransclusions.GetTransclusions(UespNamespaces.Lore);
			//// allTransclusions.GetTitles("Arena:Ria Silmane");
			allTransclusions.RemoveNamespaces(true, UespNamespaces.User, UespNamespaces.Lore);
			allTransclusions.Sort();

			return allTransclusions;
		}
		#endregion
	}
}
