namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;
	using RobinHood70.WikiClasses.Parser;
	using RobinHood70.WikiCommon;

	public class UpdateLoreBookLists : EditJob
	{
		#region Fields
		private readonly Dictionary<string, string> linkTitles = new Dictionary<string, string>();
		private readonly Dictionary<string, List<string>> pageBooks = new Dictionary<string, List<string>>();
		#endregion

		#region Constructors
		[JobInfo("Update Lore Book Entries", "Maintenance")]
		public UpdateLoreBookLists([ValidatedNotNull] Site site, AsyncInfo asyncInfo)
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
				var split = title.Split(' ', 2, StringSplitOptions.None);
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

			var goodBooks = new TitleCollection(this.Site);
			goodBooks.GetBacklinks("Template:Lore Book Compilation", BacklinksTypes.EmbeddedIn);

			var allBooks = new PageCollection(this.Site);
			allBooks.GetBacklinks("Template:Lore Book", BacklinksTypes.EmbeddedIn);
			foreach (var book in allBooks)
			{
				var parser = WikiTextParser.Parse(book.Text);
				if (parser.FindFirst<TemplateNode>(item => item.GetTitleValue() == "Lore Book") is TemplateNode template)
				{
					var addIt = true;
					foreach (var param in template.Parameters)
					{
						var paramName = param.NameToText();
						if (paramName == "up" || paramName == "prev")
						{
							if (!string.IsNullOrWhiteSpace(param.ValueToText()))
							{
								addIt = false;
								break;
							}
						}
					}

					if (addIt)
					{
						goodBooks.Add(book);
					}
				}
			}

			goodBooks.Sort((title1, title2) =>
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
			foreach (var book in goodBooks)
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