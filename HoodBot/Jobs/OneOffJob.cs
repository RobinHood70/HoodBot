namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;
	using System.IO;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon.Parser;

	public class OneOffJob : EditJob
	{
		#region Constructors
		[JobInfo("One-Off Job")]
		public OneOffJob([NotNull, ValidatedNotNull] Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
		}
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			var text = File.ReadAllText(Path.Combine(UespSite.GetBotFolder(), "Templates.txt"));
			var parsedText = WikiTextParser.Parse(text);
			var pageTitles = new TitleCollection(this.Site);
			foreach (var header in parsedText.FindAllLinked<HeaderNode>())
			{
				var pageName = ((HeaderNode)header.Value).GetInnerText(true);
				var section = new NodeCollection(null);
				section.AddLast(TemplateNode.FromParts("Minimal"));
				var node = header;
				while (node.Next is LinkedListNode<IWikiNode> next && !(next.Value is HeaderNode))
				{
					node = next;
					if (node.Value is TemplateNode template)
					{
						var templateName = template.GetTitleValue();
						if (templateName != "NewLine")
						{
							section.AddLast(template);
						}

						if (templateName == "Blades Item Summary")
						{
							section.AddLast(new TextNode($"\n'''{pageName}''' {{{{Huh}}}}\n"));
						}
					}
					else
					{
						section.AddLast(node.Value);
					}
				}

				var page = new Page(this.Site, UespNamespaces.Blades, pageName)
				{
					Text = WikiTextVisitor.Raw(section).Trim() + "\n\n{{Stub|Item}}"
				};
				this.Pages.Add(page);
				pageTitles.Add(page);
			}

			var exists = PageCollection.Unlimited(this.Site, PageModules.Info, false);
			exists.GetTitles(pageTitles);
			foreach (var page in exists)
			{
				if (page.Exists)
				{
					Debug.WriteLine($"{page} exists!");
					this.Pages[page.FullPageName()].PageName += " (item)";
				}
			}
		}

		protected override void Main() => this.SavePages("Create item page", false);
		#endregion
	}
}