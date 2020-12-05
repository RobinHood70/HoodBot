namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	public class UpdateLoreBookLists : EditJob
	{
		#region Private Constants
		private const string TemplateName = "Lore Book Entry";
		#endregion

		#region Fields
		private readonly Dictionary<string, string> titleOverrides = new Dictionary<string, string>(StringComparer.Ordinal);
		private readonly Dictionary<string, List<string>> pageBooks = new Dictionary<string, List<string>>(StringComparer.Ordinal);
		#endregion

		#region Constructors
		[JobInfo("Update Lore Book Entries", "Maintenance")]
		public UpdateLoreBookLists(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			this.Pages.GetBacklinks("Template:" + TemplateName, BacklinksTypes.EmbeddedIn);
			this.GetTitleOverrides();
			var listBooks = this.FilterToListBooks();
			this.GetPageBooks(listBooks);
			foreach (var page in this.Pages)
			{
				// This couldn't be done earlier, since we needed to parse information from the pages first, so we manually trigger the update here.
				this.LoreBookEntries_PageLoaded(this, page);
			}
		}

		protected override void Main() => this.SavePages("Update list", true, this.LoreBookEntries_PageLoaded);
		#endregion

		#region Private Static Methods
		private static bool ListBookValue(string value) =>
			value.Length != 0 &&
			!string.Equals(value, "no", StringComparison.OrdinalIgnoreCase) &&
			(string.Equals(value, "1", StringComparison.Ordinal) ||
			(int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intVal)
				? intVal != 0
				: !bool.TryParse(value, out var boolVal) || !boolVal));

		private static string SortableName(string title)
		{
			// For now, simply trimming the article so that cases like "Alik'r, The" don't sort after "Alik'r Survival". This may miss a few edge cases, though.
			title = title.Replace("\"", string.Empty, StringComparison.Ordinal);
			var split = title.Split(TextArrays.Space, 2, StringSplitOptions.None);
			return split.Length > 1 && split[0] is string article && (
				string.Equals(article, "A", StringComparison.Ordinal) ||
				string.Equals(article, "An", StringComparison.Ordinal) ||
				string.Equals(article, "The", StringComparison.Ordinal))
					? split[1] + "  , " + split[0] // We add extra spaces here so that, for example, "The Alik'r" will sort before "Alik'r Survival for Outsiders", but in the unlikely event of a hypothetical conflict between "The Alik'r" and "An Alik'r", they will still sort consistently.
					: title;
		}
		#endregion

		#region Private Methods

		private PageCollection FilterToListBooks()
		{
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
				if (this.titleOverrides.TryGetValue(page1, out var linkTitle))
				{
					page1 += linkTitle;
				}

				page1 = SortableName(page1);

				var page2 = title2.PageName;
				if (this.titleOverrides.TryGetValue(page2, out linkTitle))
				{
					page2 += linkTitle;
				}

				page2 = SortableName(page2);

				return string.Compare(page1, page2, StringComparison.OrdinalIgnoreCase);
			});

			return listBooks;
		}

		private void GetPageBooks(PageCollection listBooks)
		{
			var loreBookTitles = new TitleCollection(this.Site);
			foreach (var book in listBooks)
			{
				var label = SortableName(book.PageName);
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
		}

		private void GetTitleOverrides()
		{
			foreach (var page in this.Pages)
			{
				var parser = new ContextualParser(page);
				foreach (var template in parser.FindTemplates(TemplateName))
				{
					var param2 = template.Find(2);
					if (template.Find(2) is IParameterNode linkTitle)
					{
						var key = template.Find(1)?.Value.ToValue() ?? throw new InvalidOperationException();
						var value = linkTitle.Value.ToValue() ?? string.Empty;
						this.titleOverrides.Add(key, value);
					}
				}
			}
		}

		private void LoreBookEntries_PageLoaded(object sender, Page page)
		{
			var parser = new ContextualParser(page);
			var nodes = parser.Nodes;
			var factory = nodes.Factory;
			var first = nodes.FindIndex<SiteTemplateNode>(node => node.TitleValue.PageNameEquals(TemplateName));
			var last = nodes.FindLastIndex<SiteTemplateNode>(node => node.TitleValue.PageNameEquals(TemplateName));
			if (first != -1)
			{
				var newNodes = new List<IWikiNode>();
				nodes.RemoveRange(first, last + 1 - first);
				var letter = page.PageName[6..];
				var entries = this.pageBooks[letter];
				foreach (var entry in entries)
				{
					var template = factory.TemplateNodeFromParts(TemplateName, (null, entry));
					if (this.titleOverrides.TryGetValue(entry, out var linkTitle))
					{
						template.Add(linkTitle);
					}

					newNodes.Add(template);
					newNodes.Add(factory.TextNode("\n"));
				}

				if (newNodes.Count > 0)
				{
					newNodes.RemoveAt(newNodes.Count - 1);
				}

				nodes.InsertRange(first, newNodes);
				page.Text = parser.ToRaw();
			}
			else
			{
				throw new InvalidOperationException();
			}
		}
		#endregion
	}
}