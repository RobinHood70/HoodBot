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
		#region Static Fields
		public static readonly bool UseFastMethod = true;
		#endregion

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
				var links = UseFastMethod ? FilterTemplates(page) : FindLoreTransclusions(page);
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
		private static ICollection<string> FilterTemplates(Page page)
		{
			var retval = new SortedSet<string>();
			foreach (var template in page.Templates)
			{
				if (template.FullPageName == "Template:Game Book")
				{
					return retval;
				}

				if (template.Namespace == UespNamespaces.Lore)
				{
					retval.Add($"[[{template.FullPageName}|]]");
				}
			}

			return retval;
		}

		private static ICollection<string> FindLoreTransclusions(Page page)
		{
			var search = Template.FindRaw(null, @"\s*:?\s*(?i:l)ore:[^#\|}]+?", null, RegexOptions.None, 10);
			var matches = search.Matches(page.Text);
			var links = new SortedSet<string>();
			foreach (Match match in matches)
			{
				var template = Template.Parse(match.Value);
				links.Add($"[[{template.Name}|]]");
			}

			return links;
		}

		private static PageCollection GetLoreTransclusions(Site site)
		{
			var modules = PageModules.Info | (UseFastMethod ? PageModules.Templates : PageModules.Revisions);
			var allTransclusions = PageCollection.Unlimited(site, new PageLoadOptions(modules));
			allTransclusions.GetTransclusions(UespNamespaces.Lore);
			//// allTransclusions.GetTitles("Arena:Ria Silmane");
			allTransclusions.RemoveNamespaces(true, UespNamespaces.User, UespNamespaces.Lore);
			allTransclusions.Sort();

			return allTransclusions;
		}
		#endregion
	}
}
