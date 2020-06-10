namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	public class UpdateLoreBookLists : EditJob
	{
		#region Fields
		private readonly Dictionary<string, string> linkTitles = new Dictionary<string, string>();
		private readonly Dictionary<string, List<string>> pageBooks = new Dictionary<string, List<string>>();
		#endregion

		#region Constructors
		[JobInfo("Update Lore Book Entries", "Maintenance")]
		public UpdateLoreBookLists([NotNull, ValidatedNotNull] Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
		}
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			static string TrimArticle(string title)
			{
				// For now, simply trimming the article so that cases like "Alik'r, The" don't sort after "Alik'r Survival". This may miss a few edge cases, though.
				title = title.Replace("\"", string.Empty, StringComparison.Ordinal);
				var split = title.Split(TextArrays.Space, 2, StringSplitOptions.None);
				return split.Length > 1 && split[0] is string article && (article == "A" || article == "An" || article == "The")
					? split[1]
					: title;
			}

			this.Pages.GetBacklinks("Template:Lore Book Entry", BacklinksTypes.EmbeddedIn);
			foreach (var page in this.Pages)
			{
				var parser = WikiTextParser.Parse(page.Text);
				var templates = parser.FindAll<TemplateNode>(node => node.GetTitleValue() == "Lore Book Entry");
				foreach (var template in templates)
				{
					var param2 = template.FindNumberedParameter(2);
					if (template.FindNumberedParameter(2) is ParameterNode linkTitle)
					{
						this.linkTitles.Add(WikiTextVisitor.Value(template.FindNumberedParameter(1)?.Value ?? throw new InvalidOperationException()), WikiTextVisitor.Raw(linkTitle.Value));
					}
				}
			}

			var pageLoadOptions = new PageLoadOptions(PageModules.Info | PageModules.Custom, true);
			var pageCreator = new MetaTemplateCreator("listbook");
			var listBooks = new PageCollection(this.Site, pageLoadOptions, pageCreator);
			listBooks.SetLimitations(LimitationType.FilterTo, UespNamespaces.Lore);
			listBooks.GetCustomGenerator(new VariablesInput() { Variables = new[] { "listbook" } });
			for (var i = listBooks.Count - 1; i >= 0; i--)
			{
				var varPage = (VariablesPage)listBooks[i];
				var value = varPage.GetVariable("listbook");
				if (value != null && !ListBookValue(value))
				{
					listBooks.RemoveAt(i);
				}
			}

			listBooks.Sort((title1, title2) =>
			{
				var page1 = title1.PageName;
				if (this.linkTitles.TryGetValue(page1, out var linkTitle))
				{
					page1 += linkTitle;
				}

				page1 = TrimArticle(page1);

				var page2 = title2.PageName;
				if (this.linkTitles.TryGetValue(page2, out linkTitle))
				{
					page2 += linkTitle;
				}

				page2 = TrimArticle(page2);

				return string.Compare(page1, page2, StringComparison.OrdinalIgnoreCase);
			});

			var loreBookTitles = new TitleCollection(this.Site);
			foreach (var book in listBooks)
			{
				var label = TrimArticle(book.PageName);
				var letter = label.Substring(0, 1).ToUpperInvariant();
				if (!char.IsLetter(label[0]))
				{
					letter = "Numeric";
				}

				if (!this.pageBooks.TryGetValue(letter, out var titles))
				{
					titles = new List<string>();
					this.pageBooks.Add(letter, titles);
					loreBookTitles.Add("Lore:Books " + letter);
				}

				titles.Add(book.PageName);
			}

			foreach (var page in this.Pages)
			{
				this.LoreBookEntries_PageLoaded(this, page);
			}
		}

		protected override void Main() => this.SavePages("Update list", true, this.LoreBookEntries_PageLoaded);
		#endregion

		#region Private Static Methods
		private static bool ListBookValue(string value) =>
			value == "1" || (int.TryParse(value, out var intVal) ? intVal != 0 :
			bool.TryParse(value, out var boolVal) ? boolVal :
			value.ToLowerInvariant() == "no");
		#endregion

		#region Private Methods
		private void LoreBookEntries_PageLoaded(object sender, Page page)
		{
			var parser = WikiTextParser.Parse(page.Text);
			LinkedListNode<IWikiNode>? node = parser.FindFirstLinked<TemplateNode>(node => node.GetTitleValue() == "Lore Book Entry") ?? throw new InvalidOperationException();
			var first = node?.Previous ?? throw new InvalidOperationException();
			var last = parser.FindLastLinked<TemplateNode>(node => node.GetTitleValue() == "Lore Book Entry")?.Next ?? throw new InvalidOperationException();
			while (node != null && node != last)
			{
				var next = node.Next;
				parser.Remove(node);
				node = next;
			}

			var letter = page.PageName.Substring(6);
			var entries = this.pageBooks[letter];
			foreach (var entry in entries)
			{
				var template = TemplateNode.FromParts("Lore Book Entry", (null, entry));
				if (this.linkTitles.TryGetValue(entry, out var linkTitle))
				{
					template.AddParameter(linkTitle);
				}

				parser.AddBefore(last, new TextNode("\n"));
				parser.AddBefore(last, template);
			}

			parser.Remove(first.Next!);
			page.Text = WikiTextVisitor.Raw(parser);
		}
		#endregion
	}
}