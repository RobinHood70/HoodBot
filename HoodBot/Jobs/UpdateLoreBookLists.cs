namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.Globalization;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.ContextualParser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.BasicParser;

	public class UpdateLoreBookLists : EditJob
	{
		#region Fields
		private readonly Dictionary<string, string> linkTitles = new Dictionary<string, string>(StringComparer.Ordinal);
		private readonly Dictionary<string, List<string>> pageBooks = new Dictionary<string, List<string>>(StringComparer.Ordinal);
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
				return split.Length > 1 && split[0] is string article && (string.Equals(article, "A", StringComparison.Ordinal) || string.Equals(article, "An", StringComparison.Ordinal) || string.Equals(article, "The", StringComparison.Ordinal))
					? split[1]
					: title;
			}

			this.Pages.GetBacklinks("Template:Lore Book Entry", BacklinksTypes.EmbeddedIn);
			foreach (var page in this.Pages)
			{
				var parser = new Parser(page);
				foreach (var template in parser.FindTemplates("Lore Book Entry"))
				{
					var param2 = template.Find(2);
					if (template.Find(2) is ParameterNode linkTitle)
					{
						var key = template.Find(1)?.ValueToText() ?? throw new InvalidOperationException();
						var value = linkTitle.ValueToText() ?? string.Empty;
						this.linkTitles.Add(key, value);
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
			string.Equals(value, "1", StringComparison.Ordinal) ||
			(int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intVal)
				? intVal != 0
				: bool.TryParse(value, out var boolVal) ? boolVal : string.Equals(value.ToLowerInvariant(), "no", StringComparison.Ordinal));
		#endregion

		#region Private Methods
		private void LoreBookEntries_PageLoaded(object sender, Page page)
		{
			var parser = new Parser(page);
			var nodes = parser.Nodes;
			if (nodes.FindListNode<TemplateNode>(node => FullTitle.FromBacklinkNode(page.Site, node).PageNameEquals("Lore Book Entry"), true, false, null) is var node &&
				node != null &&
				node.Previous is LinkedListNode<IWikiNode> first &&
				nodes.FindListNode<TemplateNode>(node => FullTitle.FromBacklinkNode(page.Site, node).PageNameEquals("Lore Book Entry"), true, false, null)?.Next is LinkedListNode<IWikiNode> last)
			{
				while (node != null && node != last)
				{
					var next = node.Next;
					nodes.Remove(node);
					node = next;
				}

				var letter = page.PageName.Substring(6);
				var entries = this.pageBooks[letter];
				foreach (var entry in entries)
				{
					var template = TemplateNode.FromParts("Lore Book Entry", (null, entry));
					if (this.linkTitles.TryGetValue(entry, out var linkTitle))
					{
						template.Add(linkTitle);
					}

					nodes.AddBefore(last, new TextNode("\n"));
					nodes.AddBefore(last, template);
				}

				nodes.Remove(first.Next!);
				page.Text = parser.GetText();
			}
			else
			{
				throw new InvalidOperationException();
			}
		}
		#endregion
	}
}