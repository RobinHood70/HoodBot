namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;
	using System.Text.RegularExpressions;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.Eso;
	using RobinHood70.HoodBot.Parser;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiClasses.Parser;
	using RobinHood70.WikiCommon;

	public class EsoMatchIcons : EditJob
	{
		#region Static Fields
		private static readonly Regex OriginalFileFinder = new Regex(@"original file(name)?:\s*(?<name>[^<\n]+?)(<br>)?\n+(used for:\n)?(:(Achievement|Book|Collectible|Mined Item|Quest Item): .+?\n)*", RegexOptions.IgnoreCase);
		private static readonly string WikiImageFolder = Environment.ExpandEnvironmentVariables(@"%BotData%\WikiImages\");
		#endregion

		#region Fields
		private readonly HashSet<ISimpleTitle> licenseTemplates = new HashSet<ISimpleTitle>(SimpleTitleEqualityComparer.Instance);
		private IReadOnlyDictionary<string, List<ItemInfo>> allItems;
		private PageCollection pages;
		private IReadOnlyDictionary<string, ICollection<string>> allIcons;
		#endregion

		#region Constructors
		[JobInfo("Match Icons", "ESO")]
		public EsoMatchIcons(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
		}
		#endregion

		#region Public Override Properties
		public override string LogName => "Match ON-icons with log viewer";
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			this.StatusWriteLine("Saving pages");
			this.Pages.RemoveUnchanged();
			this.ProgressMaximum = this.Pages.Count;
			this.EditConflictAction = this.Pages_PageLoaded;
			foreach (var page in this.Pages)
			{
				this.SavePage(page, this.LogName, true);
				this.Progress++;
			}
		}

		protected override void PrepareJob()
		{
			this.StatusWriteLine("Getting database info");
			this.allItems = this.GetItems();

			this.StatusWriteLine("Calculating image checksums");
			this.allIcons = GetIconChecksums();

			this.StatusWriteLine("Getting image info from wiki");
			this.GetLicenseTemplates();

			this.Pages.SetLimitations(LimitationType.FilterTo, MediaWikiNamespaces.File);
			this.Pages.PageLoaded += this.Pages_PageLoaded;
			this.Pages.GetNamespace(MediaWikiNamespaces.File, Filter.Exclude, "ON-icon-");
			//// this.FixBotError();
			//// this.pages.GetTitles("File:ON-icon-dye stamp-Blossoming Darkness, Sunlight.png");
			this.Pages.PageLoaded -= this.Pages_PageLoaded;

			this.Pages.Sort();
		}
		#endregion

		#region Private Static Methods
		private static IReadOnlyDictionary<string, ICollection<string>> GetIconChecksums()
		{
			var iconFolder = WikiImageFolder + @"icons\";
			var allIcons = new Dictionary<string, ICollection<string>>();
			if (Directory.Exists(iconFolder))
			{
				foreach (var file in Directory.EnumerateFiles(iconFolder, "*.*", SearchOption.AllDirectories))
				{
					var fileData = File.ReadAllBytes(file);
					var checksum = Globals.GetHash(fileData, HashType.Sha1);
					if (!allIcons.TryGetValue(checksum, out var list))
					{
						list = new HashSet<string>(1);
						allIcons.Add(checksum, list);
					}

					list.Add(file.Substring(iconFolder.Length).Replace(".png", string.Empty).Replace('\\', '/'));
				}
			}

			return allIcons;
		}

		private static void LoadQueryData(Dictionary<string, List<ItemInfo>> entries, KeyValuePair<string, string> query)
		{
			foreach (var row in EsoGeneral.RunQuery(query.Value))
			{
				long id;
				var idType = row.GetDataTypeName(0);
				if (idType == "INT")
				{
					id = (int)row["id"];
				}
				else if (idType == "BIGINT")
				{
					id = (long)row["id"];
				}
				else
				{
					throw new InvalidCastException();
				}

				var iconName = ((string)row["icon"]).Replace("/esoui/art/icons/", string.Empty).Replace(".dds", string.Empty);
				var entry = new ItemInfo(
					id: id,
					itemName: (string)row["name"],
					type: query.Key);
				if (!entries.TryGetValue(iconName, out var list))
				{
					list = new List<ItemInfo>(1);
					entries.Add(iconName, list);
				}

				list.Add(entry);
			}
		}
		#endregion

		#region Private Methods
		/*
		private void FixBotError()
		{
			var from = new DateTime(2019, 10, 31, 8, 38, 0, DateTimeKind.Utc);
			var to = null as DateTime?; // new DateTime(2019, 10, 29, 10, 42, 10, DateTimeKind.Utc);
			var user = this.Site.User;
			var contributions = user.GetContributions(from, to);
			var revIds = new SortedSet<long>();
			foreach (var contribution in contributions)
			{
				revIds.Add(contribution.ParentId);
				revIds.Add(contribution.Id);
			}

			this.pages.GetRevisionIds(revIds);
			foreach (var page in this.pages)
			{
				var filePage = page as FilePage;
				var latest = page.Revisions.Current;
				if (latest.Text.IndexOf("original file:", StringComparison.OrdinalIgnoreCase) != -1 && filePage.LatestFileRevision is FileRevision fr)
				{
					if (!this.allIcons.ContainsKey(fr.Sha1))
					{
						Debug.WriteLine(page.FullPageName);
					}
				}
			}
		}
		*/

		private IReadOnlyDictionary<string, List<ItemInfo>> GetItems()
		{
			var queries = new Dictionary<string, string>
			{
				["achievements"] = "SELECT id, name, icon FROM achievements WHERE icon != '/esoui/art/icons/icon_missing.dds'",
				["book"] = "SELECT id, title name, icon FROM book WHERE icon != '/esoui/art/icons/icon_missing.dds'",
				["collectibles"] = "SELECT id, name, icon FROM collectibles WHERE icon != '/esoui/art/icons/icon_missing.dds'",
				["minedItemSummary"] = "SELECT MIN(itemId) id, name, icon FROM minedItemSummary WHERE icon != '/esoui/art/icons/icon_missing.dds' GROUP BY name, icon",
				["questItem"] = "SELECT questId id, questName name, icon FROM questItem WHERE icon != '/esoui/art/icons/icon_missing.dds'",
				["skillTree"] = "SELECT id, name, icon FROM skillTree WHERE icon != ''",
			};

			var entries = new Dictionary<string, List<ItemInfo>>();
			foreach (var query in queries)
			{
				LoadQueryData(entries, query);
			}

			return entries;
		}

		private void GetLicenseTemplates()
		{
			var licenseTemplates = new TitleCollection(this.Site);
			licenseTemplates.GetCategoryMembers("Image Copyright Templates");
			licenseTemplates.Remove("Template:Zenimage");
			foreach (var template in licenseTemplates)
			{
				this.licenseTemplates.Add(template);
			}
		}

		private string GetUsageList(Page page, IReadOnlyCollection<string> allNames)
		{
			var isBook = false;
			foreach (var category in page.Categories)
			{
				if (category.PageName == "Online-Icons-Books" ||
					(category.PageName == "Online-Icons-Quest Items" && page.PageName.Contains("Book")) ||
					page.PageName == "ON-icon-minor adornment-Necklace.png" ||
					page.PageName == "ON-icon-minor adornment-Ring.png")
				{
					isBook = true;
					break;
				}
			}

			var matchedNames = new SortedDictionary<string, List<ItemInfo>>();
			var unmatchedNames = new SortedSet<string>();
			var entryList = new List<string>();
			foreach (var name in allNames)
			{
				if (isBook || !this.allItems.TryGetValue(name, out var iconItems) || iconItems.Count <= 0)
				{
					unmatchedNames.Add(name);
				}
				else
				{
					matchedNames.Add(name, iconItems);
				}
			}

			if (!isBook)
			{
				foreach (var icon in matchedNames)
				{
					entryList.Add($"Original file: {icon.Key}<br>\nUsed for:");
					foreach (var item in icon.Value)
					{
						var text = item.Type switch
						{
							"achievements" => "\n:Achievement: " + item.ItemName,
							"book" => "\n:Book: " + item.ItemName,
							"collectibles" => $"\n:Collectible: {{{{Item Link|{item.ItemName}|collectid={item.Id}}}}}",
							"minedItemSummary" => $"\n:Mined Item: {{{{Item Link|{item.ItemName}|itemid={item.Id}}}}}",
							"questItem" => $"\n:Quest Item: " + item.ItemName,
							_ => null,
						};

						if (text != null && !entryList.Contains(text))
						{
							entryList.Add(text);
						}
					}

					entryList.Add("\n\n");
				}
			}

			var sb = new StringBuilder();
			if (unmatchedNames.Count > 0)
			{
				sb
					.Append("Original file: ")
					.Append(string.Join(", ", unmatchedNames))
					.Append("\n\n");
			}

			if (matchedNames.Count > 0)
			{
				sb.Append(string.Join(string.Empty, entryList));
			}

			return sb.ToString().Trim() + "\n\n";
		}

		private void Pages_PageLoaded(object sender, Page page)
		{
			/*
			var oldest = page.Revisions.Current;
			foreach (var rev in page.Revisions)
			{
				if (rev.Id < oldest.Id)
				{
					oldest = rev;
				}
			}

			page.Text = oldest.Text; */
			if (page is FilePage filePage && filePage.LatestFileRevision is FileRevision latestRevision)
			{
				page.Text = page.Text.Trim().Replace("Original file: Original file:", "Original file:");
				this.allIcons.TryGetValue(latestRevision.Sha1, out var foundIcons);
				if (foundIcons == null)
				{
					var parsedPage = ContextualParser.FromPage(page);
					this.ReplaceNoMatch(parsedPage);
					parsedPage.Replace(this.ReplaceLicense);
					page.Text = WikiTextVisitor.Raw(parsedPage);
				}
				else
				{
					var list = new SortedSet<string>();
					var match = OriginalFileFinder.Match(page.Text);
					while (match.Success)
					{
						var split = match.Groups["name"].Value.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
						for (var i = 0; i < split.Length; i++)
						{
							split[i] = split[i].Trim().Replace(".png", string.Empty).Replace(".dds", string.Empty);
						}

						list.AddRange(split);
						page.Text = page.Text.Remove(match.Index, match.Length);
						match = OriginalFileFinder.Match(page.Text);
					}

					foreach (var icon in foundIcons)
					{
						list.Add(icon);
					}

					var parsedPage = ContextualParser.FromPage(page);
					var newText = this.GetUsageList(page, list);
					this.ReplaceNormal(parsedPage, newText);
					parsedPage.Replace(this.ReplaceLicense);
					page.Text = WikiTextVisitor.Raw(parsedPage);
				}
			}
		}

		private IWikiNode ReplaceLicense(LinkedListNode<IWikiNode> node)
		{
			if (node.Value is TemplateNode template)
			{
				var templateName = Title.DefaultToNamespace(this.Site, MediaWikiNamespaces.Template, WikiTextVisitor.Value(template.Title));
				if (this.licenseTemplates.Contains(templateName))
				{
					return TemplateNode.FromParts("Zenimage");
				}
			}

			return node.Value;
		}

		private void ReplaceNoMatch(ContextualParser parsedPage)
		{
			var filePage = parsedPage.Title as Page;
			foreach (var pageCat in filePage.Categories)
			{
				if (pageCat.PageName == "Online-Icons-Missing Original File")
				{
					return;
				}
			}

			var site = this.Site;
			var category = parsedPage.FindLastLinked<LinkNode>(item =>
			{
				// While this could be done as a one-liner, it would be fragile and not easy to debug.
				var linkTitle = WikiTextVisitor.Value(item.Title);
				var title = new Title(site, linkTitle);
				return title.Namespace == MediaWikiNamespaces.Category;
			});

			if (category != null && category.Previous?.Value is TextNode textNode && (textNode?.Text?.EndsWith("\n", StringComparison.Ordinal) == true))
			{
				category = parsedPage.AddAfter(category, new TextNode("\n"));
			}

			var addAfterNode = category ?? parsedPage.Last;
			parsedPage.AddAfter(addAfterNode, LinkNode.FromParts("Category:Online-Icons-Missing Original File"));
		}

		private void ReplaceNormal(ContextualParser parsedPage, string newText)
		{
			var addAfter = parsedPage.FindFirstLinked<HeaderNode>(item => item.GetInnerText(true) == "Summary");
			if (addAfter == null)
			{
				addAfter = parsedPage.AddFirst(HeaderNode.FromText("== Summary =="));
				parsedPage.AddAfter(addAfter, new TextNode("\n"));
			}

			do
			{
				addAfter = addAfter.Next;
			}
			while (addAfter != null && !(addAfter.Value is HeaderNode) && !(addAfter.Value is LinkNode link && new Title(this.Site, WikiTextVisitor.Value(link.Title)).Namespace == MediaWikiNamespaces.Category));

			addAfter = addAfter == null ? parsedPage.Last : addAfter.Previous;
			if (!(addAfter.Value is TextNode textNode))
			{
				textNode = new TextNode(newText);
				parsedPage.AddAfter(addAfter, textNode);
			}

			textNode.Text = textNode.Text.TrimEnd();
			textNode.Text += (textNode.Text.Length == 0) ? "\n" : "\n\n";
			textNode.Text += newText;
		}
		#endregion

		#region Private Classes
		private struct ItemInfo
		{
			public ItemInfo(long id, string itemName, string type)
			{
				this.Id = id;
				this.ItemName = itemName;
				this.Type = type;
			}

			public long Id { get; }

			public string ItemName { get; }

			public string Type { get; }
		}
		#endregion
	}
}