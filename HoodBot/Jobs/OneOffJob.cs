namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Parser;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon.Parser;
	using static RobinHood70.CommonCode.Globals;

	public class OneOffJob : ParsedPageJob
	{
		#region Fields
		private readonly Dictionary<string, bool> headers = new Dictionary<string, bool>();
		#endregion

		#region Constructors
		[JobInfo("One-Off Job")]
		public OneOffJob([NotNull, ValidatedNotNull] Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
		}
		#endregion

		#region Protected Override Properties
		protected override string EditSummary => "Update link to Aquatic Animals";
		#endregion

		#region Protected Override Methods
		protected override void JobCompleted()
		{
			var first = true;
			foreach (var header in this.headers)
			{
				if (!header.Value)
				{
					if (first)
					{
						this.WriteLine("== Aquatic Animals with no Redirects ==");
						this.WriteLine("The following entries were not valid Lore-space redirects:");
						first = false;
					}

					this.WriteLine($"* {header.Key}");
				}
			}

			base.JobCompleted();
		}

		protected override void LoadPages()
		{
			var titles = new TitleCollection(this.Site);
			var aquatic = this.Site.LoadPageText("Lore:Aquatic Animals");
			var parsed = WikiTextParser.Parse(aquatic);
			foreach (var header in parsed.FindAll<HeaderNode>(headerNode => headerNode.Level == 2))
			{
				var title = header.GetInnerText(true);
				this.headers.Add(title, false);
				titles.Add(new Title(this.Site, UespNamespaces.Lore, title, true));
			}

			this.Pages.GetTitles(titles);
		}

		protected override void ParseText(object sender, Page page, ContextualParser parsedPage)
		{
			ThrowNull(page, nameof(page));
			ThrowNull(parsedPage, nameof(parsedPage));
			if (page.IsRedirect && parsedPage.FindFirst<LinkNode>() is LinkNode redirect)
			{
				var title = new FullTitle(this.Site, WikiTextVisitor.Value(redirect.Title))
				{
					NamespaceId = UespNamespaces.Lore,
					PageName = "Aquatic Animals",
					Fragment = page.PageName
				};
				redirect.Title.Clear();
				redirect.Title.AddText(title.ToString());
				this.headers[page.PageName] = true;
			}
		}
		#endregion
	}
}