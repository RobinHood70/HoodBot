namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using RobinHood70.CommonCode;

	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	[method: JobInfo("Update Lore Book Entries", "Maintenance")]
	public class UpdateLoreBookLists(JobManager jobManager) : EditJob(jobManager)
	{
		#region Private Constants
		private const string TemplateName = "Lore Book Entry";
		#endregion

		#region Fields
		private readonly Dictionary<string, string> titleOverrides = new(StringComparer.Ordinal);
		private readonly Dictionary<string, List<string>> pageBooks = new(StringComparer.Ordinal);
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Update list";

		protected override void LoadPages()
		{
			var loreBookPages = new PageCollection(this.Site);
			loreBookPages.GetBacklinks("Template:" + TemplateName, BacklinksTypes.EmbeddedIn);
			foreach (var page in loreBookPages)
			{
				SiteParser parser = new(page);
				foreach (var template in parser.FindSiteTemplates(TemplateName))
				{
					if (template.GetValue(2) is string value)
					{
						var key = template.GetValue(1) ?? throw new InvalidOperationException();
						this.titleOverrides.Add(key, value);
					}
				}
			}

			var listBooks = this.FilterToListBooks();
			this.GetPageBooks(listBooks);
			foreach (var page in loreBookPages)
			{
				// Add and update manually since we need to parse information from the pages first.
				this.Pages.Add(page);
			}
		}

		protected override void PageLoaded(Page page)
		{
			SiteParser parser = new(page);
			var factory = parser.Factory;
			var first = parser.FindIndex<SiteTemplateNode>(node => node.Title.PageNameEquals(TemplateName));
			var last = parser.FindLastIndex<SiteTemplateNode>(node => node.Title.PageNameEquals(TemplateName));
			if (first != -1)
			{
				List<IWikiNode> newNodes = [];
				parser.RemoveRange(first, last + 1 - first);
				var letter = page.Title.PageName[6..];
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

				parser.InsertRange(first, newNodes);
				parser.UpdatePage();
			}
			else
			{
				throw new InvalidOperationException();
			}
		}
		#endregion

		#region Private Static Methods
		private static bool ListBookValue(string value) =>
			value.Length != 0 &&
			!string.Equals(value, "no", StringComparison.OrdinalIgnoreCase) &&
			(value.OrdinalEquals("1") ||
			(int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intVal)
				? intVal != 0
				: !bool.TryParse(value, out var boolVal) || !boolVal));

		private static string SortableName(string title)
		{
			// For now, simply trimming the article so that cases like "Alik'r, The" don't sort after "Alik'r Survival". This may miss a few edge cases, though.
			title = title.Replace("\"", string.Empty, StringComparison.Ordinal);
			var split = title.Split(TextArrays.Space, 2, StringSplitOptions.None);
			return split.Length > 1 && split[0] is string article && (
				article.OrdinalEquals("A") ||
				article.OrdinalEquals("An") ||
				article.OrdinalEquals("The"))
					? split[1] + "  , " + split[0] // We add extra spaces here so that, for example, "The Alik'r" will sort before "Alik'r Survival for Outsiders", but in the unlikely event of a hypothetical conflict between "The Alik'r" and "An Alik'r", they will still sort consistently.
					: title;
		}
		#endregion

		#region Private Methods

		private PageCollection FilterToListBooks()
		{
			var listBooks = this.Site.CreateMetaPageCollection(PageModules.Info, false, "listbook");
			listBooks.SetLimitations(LimitationType.OnlyAllow, UespNamespaces.Lore);
			listBooks.GetCustomGenerator(new VariablesInput() { Variables = ["listbook"] });
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
				var page1 = title1.Title.PageName;
				if (this.titleOverrides.TryGetValue(page1, out var linkTitle))
				{
					page1 += linkTitle;
				}

				page1 = SortableName(page1);

				var page2 = title2.Title.PageName;
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
			TitleCollection loreBookTitles = new(this.Site);
			foreach (var book in listBooks)
			{
				var label = SortableName(book.Title.PageName);
				var letter = label[..1].ToUpperInvariant();
				if (!char.IsLetter(label[0]))
				{
					letter = "Numeric";
				}

				if (!this.pageBooks.TryGetValue(letter, out var titles))
				{
					titles = [];
					this.pageBooks.Add(letter, titles);
					loreBookTitles.Add("Lore:Books " + letter);
				}

				titles.Add(book.Title.PageName);
			}
		}
		#endregion
	}
}