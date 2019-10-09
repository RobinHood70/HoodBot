namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Diagnostics;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.WikiClasses.Parser;
	using RobinHood70.WikiCommon;

	public class UntranscludeLore : EditJob
	{
		#region Constructors
		[JobInfo("Untransclude Lore")]
		public UntranscludeLore([ValidatedNotNull] Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
		}
		#endregion

		#region Public Override Properties
		public override string LogName => "Untransclude Lore";
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
		}

		protected override void PrepareJob()
		{
			var (gamePages, lorePages) = this.GetPages();
			foreach (var page in gamePages)
			{
				var parsedText = WikiTextParser.Parse(page.Text);
				foreach (var node in parsedText)
				{
					if (node is TemplateNode templateNode)
					{
						var templateTitle = WikiTextVisitor.Value(templateNode.Title).Trim();
						if (templateTitle.Contains("ore:"))
						{
							if (!lorePages.TryGetValue(templateTitle, out var lorePage))
							{
								throw new InvalidOperationException();
							}

							var loreParser = WikiTextParser.Parse(lorePage.Text, true, true);
							var call = WikiTextVisitor.Raw(templateNode);
							Debug.WriteLine(call);

							// Debug.WriteLine(new string('=', call.Length));
							// Debug.WriteLine(this.wikiTextRaw.Build(loreParser));
						}
					}
				}
			}
		}
		#endregion

		#region Private Methods
		private (PageCollection gamePages, PageCollection lorePages) GetPages()
		{
			var allPages = new PageCollection(this.Site);
			allPages.GetPageLinks(new[] { new Title(this.Site, this.Site.User.FullPageName + "/Lore Transclusions") });
			allPages.Sort();

			var lorePages = new PageCollection(this.Site);
			var gamePages = new PageCollection(this.Site);

			foreach (var page in allPages)
			{
				if (page.Namespace == UespNamespaces.Lore)
				{
					lorePages.Add(page);
				}
				else
				{
					gamePages.Add(page);
				}
			}

			return (gamePages, lorePages);
		}
		#endregion
	}
}
