namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	public class EsoCreateCollectibleSummaries : EditJob
	{
		#region Constants
		private const string CollectibleType = "Pet";
		private const string CollectibleFragment = "pet";
		private const string CollectibleTypePrefix = CollectibleType + "s ";
		private const string CollectibleTemplate =
			"{{Minimal|" + CollectibleType + "}}{{Online Collectible Summary\n" +
			"|image=ON-" + CollectibleFragment + "-\n" +
			"|icon=ON-icon-" + CollectibleFragment + "-\n" +
			"|id=\n" +
			"|description=\n" +
			"|collectibletype=" + CollectibleType + "\n" +
			"|type=\n" +
			"|name=\n" +
			"|price=\n" +
			"|acquisition=\n" +
			"|crate=\n" +
			"|tier=\n" +
			"|date=\n" +
			"}}\n" +
			"{{NewLeft}}\n" +
			"<!--==Occurrences==\n" +
			"{{scrollbox\n" +
			"| width=70%\n" +
			"| height=100px\n" +
			"| content =\n" +
			"'''Appearances: '''\n" +
			"* - \n" +
			"}}\n" +
			"--><!--== Physical Description==\n\n" +
			"--><!--==Gallery==\n" +
			"<gallery>\n" +
			"File:ON-crown store-.jpg|[[Online:Crown Store Renders/Mounts|Promotional render]]\n" +
			"</gallery>--><!--\n\n" +
			"==Notes==\n" +
			"*\n" +
			"--><!--Instructions: Add any bugs related to the collectible here using the format below.\n" +
			"==Bugs==\n" +
			"{{Bug|Bug description}}\n" +
			"**Workaround\n" +
			"--><!--\n" +
			"== References ==\n" +
			"<references />-->\n" +
			"{{Stub|Collectible}}";
		#endregion

		#region Static Fields
		private static readonly List<string> IgnoredHeaders = new()
		{
			"Bugs",
			"Notes",
			"References",
			"See Also"
		};
		#endregion

		#region Fields
		private PageCollection sourcePages;
		#endregion

		#region Constructors
		[JobInfo("Create Collectible Summaries", "ESO")]
		public EsoCreateCollectibleSummaries(JobManager jobManager)
			: base(jobManager) => this.sourcePages = new PageCollection(jobManager.Site);
		#endregion

		#region Protected Override Properties
		public override string LogName => "Create Collectible Summaries";
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			this.sourcePages = new PageCollection(this.Site);
			this.sourcePages.PageLoaded += AddHeaderLinks;
			this.sourcePages.GetNamespace(UespNamespaces.Online, Filter.Exclude, CollectibleTypePrefix);
			this.sourcePages.PageLoaded -= AddHeaderLinks;

			var titles = new TitleCollection(this.Site);
			foreach (var page in this.sourcePages)
			{
				var parsedPage = new ContextualParser(page);
				foreach (var headerNode in parsedPage.HeaderNodes)
				{
					if (headerNode.Level == 3 &&
						!IgnoredHeaders.Contains(headerNode.GetInnerText(true), System.StringComparer.OrdinalIgnoreCase))
					{
						foreach (var link in headerNode.Title.FindAll<SiteLinkNode>())
						{
							titles.Add(link.TitleValue);
						}
					}
				}
			}

			this.Pages.PageLoaded += CreateCollectiblePage;
			this.Pages.GetTitles(titles);
			this.Pages.PageLoaded -= CreateCollectiblePage;
		}

		protected override void Main()
		{
			this.SavePages("Create Collectible page", false, CreateCollectiblePage);
			this.SavePages(this.sourcePages, $"Saving {CollectibleType} pages", "Add links to headers", true, AddHeaderLinks);
		}
		#endregion

		#region Private Methods
		private static void AddHeaderLinks(object sender, Page page)
		{
			var parsedPage = new ContextualParser(page);
			var factory = parsedPage.Nodes.Factory;
			foreach (var headerNode in parsedPage.HeaderNodes)
			{
				if (headerNode.Title.Count == 1 && headerNode.Title[0] is ITextNode textNode)
				{
					var headerText = textNode.Text.Trim(TextArrays.EqualsSign);
					if (headerText.Length > 0)
					{
						// Deliberately only retains one space before and after if there are multiples.
						var spaceBefore = headerText[0] == ' ' ? " " : string.Empty;
						var spaceAfter = headerText[^1] == ' ' ? " " : string.Empty;
						headerText = headerText.Trim();
						headerNode.Title.Clear();
						headerNode.Title.AddRange(
							factory.TextNode(new string('=', headerNode.Level) + spaceBefore),
							factory.LinkNodeFromParts("Online:" + headerText, headerText),
							factory.TextNode(spaceAfter + new string('=', headerNode.Level)));
					}
				}
			}

			page.Text = parsedPage.ToRaw();
		}

		private static void CreateCollectiblePage(object sender, Page page)
		{
			if (!page.Exists || page.IsRedirect)
			{
				page.Text = CollectibleTemplate;
			}
		}
		#endregion
	}
}