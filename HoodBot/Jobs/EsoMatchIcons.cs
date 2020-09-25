namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;
	using RobinHood70.WikiCommon.Parser.Basic;
	using static RobinHood70.CommonCode.Globals;

	public class EsoMatchIcons : EditJob
	{
		#region Static Fields
		private static readonly DateTime LastRun = new DateTime(2019, 10, 28);
		private static readonly Regex OriginalFileFinder = new Regex(@"(https?://|Original file(name)?:\s*)(?<name>[^<\n]+?)(<br>)?\n+(used for:\s*\n)?(:?(Achievement|Book|Collectible|Mined Item|Quest Item): .+?\n)*", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, DefaultRegexTimeout);
		private static readonly string WikiImageFolder = Environment.ExpandEnvironmentVariables(@"%BotData%\WikiImages\"); // Files in this folder come from http://esofiles.uesp.net/update-<whatever>/icons.zip
		#endregion

		#region Fields
		private readonly HashSet<Title> licenseTemplates = new HashSet<Title>();
		private readonly Dictionary<string, List<ItemInfo>> allItems = new Dictionary<string, List<ItemInfo>>(StringComparer.Ordinal);
		private readonly Dictionary<string, ICollection<string>> allIcons = new Dictionary<string, ICollection<string>>(StringComparer.Ordinal);
		#endregion

		#region Constructors
		[JobInfo("Match Icons", "ESO")]
		public EsoMatchIcons(JobManager jobManager)
			: base(jobManager)
		{
			this.Pages.LoadOptions = new PageLoadOptions(PageModules.Info | PageModules.Revisions | PageModules.FileInfo)
			{
				RevisionFrom = new DateTime(2020, 09, 25, 00, 53, 36, DateTimeKind.Utc),
				RevisionNewer = false,
			};
			this.Pages.SetLimitations(LimitationType.FilterTo, MediaWikiNamespaces.File);
		}
		#endregion

		#region Public Override Properties
		public override string LogName => "Match ON-icons with log viewer";
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			this.StatusWriteLine("Getting database info");
			this.GetItems();

			this.StatusWriteLine("Calculating image checksums");
			this.GetIconChecksums();

			this.StatusWriteLine("Getting image info from wiki");
			this.GetLicenseTemplates();

			var titles = new TitleCollection(this.Site);
			titles.GetNamespace(MediaWikiNamespaces.File, Filter.Exclude, "ON-icon-");

			this.Pages.PageLoaded += this.Pages_PageLoaded;
			this.Pages.GetTitles(titles);
			// this.Pages.GetNamespace(MediaWikiNamespaces.File, Filter.Exclude, "ON-icon-");
			this.Pages.PageLoaded -= this.Pages_PageLoaded;

			this.Pages.Sort();
		}

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
		#endregion

		#region Private Static Methods
		private static bool IsBook(Page page)
		{
			var isBook = false;
			foreach (var category in page.Categories)
			{
				if (category.PageNameEquals("Online-Icons-Books") ||
					(category.PageNameEquals("Online-Icons-Quest Items") && page.PageName.Contains("Book", StringComparison.Ordinal)) ||
					page.PageNameEquals("ON-icon-minor adornment-Necklace.png") ||
					page.PageNameEquals("ON-icon-minor adornment-Ring.png"))
				{
					isBook = true;
					break;
				}
			}

			return isBook;
		}

		private static void ReplaceNormal(ContextualParser parser, int index, string newText)
		{
			if (index == parser.Nodes.Count || !(parser.Nodes[index] is ITextNode textNode))
			{
				textNode = parser.Nodes.Factory.TextNode(string.Empty);
				parser.Nodes.Insert(index, textNode);
			}

			textNode.Text = textNode.Text.TrimEnd();
			textNode.Text += (textNode.Text.Length == 0) ? "\n" : "\n\n";
			textNode.Text += newText;
		}

		private static void ReplaceNoMatch(ContextualParser parser)
		{
			var categoryIndex = parser.Nodes.FindLastIndex<SiteLinkNode>(link => link.TitleValue.Namespace == MediaWikiNamespaces.Category) + 1;
			if (categoryIndex == 0)
			{
				categoryIndex = parser.Nodes.Count;
			}

			if (parser.Nodes.Find<SiteLinkNode>(item => item.TitleValue.PageNameEquals("Online-Icons-Missing Original File")) == null)
			{
				parser.Nodes.InsertRange(categoryIndex, new IWikiNode[]
				{
					parser.Nodes.Factory.TextNode("\n"),
					parser.Nodes.Factory.LinkNodeFromParts("Category:Online-Icons-Missing Original File"),
				});
			}
		}
		#endregion

		#region Private Methods
		private void GetIconChecksums()
		{
			var iconFolder = WikiImageFolder;
			if (Directory.Exists(iconFolder))
			{
				foreach (var file in Directory.EnumerateFiles(iconFolder, "*.*", SearchOption.AllDirectories))
				{
					var fileData = File.ReadAllBytes(file);
					var checksum = GetHash(fileData, HashType.Sha1);
					if (!this.allIcons.TryGetValue(checksum, out var list))
					{
						list = new HashSet<string>(1, StringComparer.Ordinal);
						this.allIcons.Add(checksum, list);
					}

					list.Add(file.Substring(iconFolder.Length).Replace(".png", string.Empty, StringComparison.OrdinalIgnoreCase).Replace('\\', '/'));
				}
			}
		}

		private void GetItems()
		{
			var queries = new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["achievements"] = "SELECT id, name, icon FROM achievements WHERE icon != '/esoui/art/icons/icon_missing.dds'",
				["book"] = "SELECT id, title name, icon FROM book WHERE icon != '/esoui/art/icons/icon_missing.dds'",
				["collectibles"] = "SELECT id, name, icon FROM collectibles WHERE icon != '/esoui/art/icons/icon_missing.dds'",
				["minedItemSummary"] = "SELECT MIN(itemId) id, name, icon FROM minedItemSummary WHERE icon != '/esoui/art/icons/icon_missing.dds' GROUP BY name, icon",
				["questItem"] = "SELECT questId id, questName name, icon FROM questItem WHERE icon != '/esoui/art/icons/icon_missing.dds'",
				["skillTree"] = "SELECT id, name, icon FROM skillTree WHERE icon != ''",
			};

			foreach (var query in queries)
			{
				this.LoadQueryData(query);
			}
		}

		private void GetLicenseTemplates()
		{
			var copyrightTemplates = new TitleCollection(this.Site);
			copyrightTemplates.GetCategoryMembers("Image Copyright Templates");
			copyrightTemplates.Remove("Template:Zenimage");
			foreach (var template in copyrightTemplates)
			{
				this.licenseTemplates.Add(template);
			}
		}

		private string GetUsageList(bool isBook, IReadOnlyCollection<string> allNames)
		{
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
					var sorted = new SortedSet<string>(StringComparer.Ordinal);
					foreach (var item in icon.Value)
					{
						var text = item.Type switch
						{
							"achievements" => "\n:Achievement: " + item.ItemName,
							"book" => "\n:Book: " + item.ItemName,
							"collectibles" => $"\n:Collectible: {{{{Item Link|{item.ItemName}|collectid={item.Id.ToStringInvariant()}}}}}",
							"minedItemSummary" => $"\n:Mined Item: {{{{Item Link|{item.ItemName}|itemid={item.Id.ToStringInvariant()}}}}}",
							"questItem" => $"\n:Quest Item: " + item.ItemName,
							_ => null,
						};

						if (text != null && !entryList.Contains(text))
						{
							sorted.Add(text);
						}
					}

					entryList.AddRange(sorted);
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

		private void LoadQueryData(KeyValuePair<string, string> query)
		{
			foreach (var row in EsoGeneral.RunQuery(query.Value))
			{
				long id;
				var idType = row.GetDataTypeName(0);
				id =
					string.Equals(idType, "INT", StringComparison.Ordinal) ? (int)row["id"] :
					string.Equals(idType, "BIGINT", StringComparison.Ordinal) ? (long)row["id"] :
					throw new InvalidCastException();

				var iconName = ((string)row["icon"]).Replace("/esoui/art/icons/", string.Empty, StringComparison.OrdinalIgnoreCase).Replace(".dds", string.Empty, StringComparison.OrdinalIgnoreCase);
				var entry = new ItemInfo(
					id: id,
					itemName: (string)row["name"],
					type: query.Key);
				if (!this.allItems.TryGetValue(iconName, out var list))
				{
					list = new List<ItemInfo>(1);
					this.allItems.Add(iconName, list);
				}

				list.Add(entry);
			}
		}

		private void Pages_PageLoaded(object sender, Page page)
		{
			if (page.Revisions.Count == 0)
			{
				return;
			}

			page.Text = page.Revisions[0].Text; // Temporary for history load
			if (page is FilePage filePage && filePage.LatestFileRevision is FileRevision latestRevision)
			{
				this.allIcons.TryGetValue(latestRevision.Sha1 ?? throw PropertyNull(nameof(latestRevision), nameof(latestRevision.Sha1)), out var foundIcons);
				var parser = new ContextualParser(page);
				if (foundIcons == null || foundIcons.Count == 0)
				{
					if (parser.Nodes.Find<TextNode>(textNode => textNode.Text.Contains("Original file", StringComparison.OrdinalIgnoreCase)) != null)
					{
						return;
					}

					if (latestRevision.Timestamp > LastRun)
					{
						ReplaceNoMatch(parser);
					}
				}
				else
				{
					var summaryIndex = parser.IndexOfHeader("Summary") + 1;
					if (summaryIndex == 0)
					{
						parser.Nodes.InsertRange(0, new IWikiNode[]
						{
							parser.Nodes.Factory.HeaderNodeFromParts(2, "Summary"),
						});

						summaryIndex = 1;
					}

					var summaryEnd = parser.Nodes.FindIndex<HeaderNode>(summaryIndex);
					if (summaryEnd == -1)
					{
						summaryEnd = parser.Nodes.Count;
					}

					// Move categories to end, then parse out the text.
					var beforeCats = summaryEnd;
					for (var i = summaryIndex; i < beforeCats; i++)
					{
						if (parser.Nodes[i] is SiteLinkNode link && link.TitleValue.Namespace == MediaWikiNamespaces.Category)
						{
							parser.Nodes.InsertRange(summaryEnd, new IWikiNode[]
							{
								parser.Nodes[i],
								parser.Nodes.Factory.TextNode("\n"),
							});

							parser.Nodes.RemoveAt(i);
							beforeCats--;
							summaryEnd++;
						}
					}

					var text = WikiTextVisitor.Raw(parser.Nodes.GetRange(summaryIndex, beforeCats - summaryIndex)).Trim() + "\n";
					parser.Nodes.RemoveRange(summaryIndex, beforeCats - summaryIndex);

					var list = new SortedSet<string>();
					var match = OriginalFileFinder.Match(text);
					while (match.Success)
					{
						var split = match.Groups["name"].Value.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
						for (var i = 0; i < split.Length; i++)
						{
							var name = split[i]
								.Trim(TextArrays.SquareBrackets)
								.Split(TextArrays.Space, 2)[0]
								.Split("esoicons.uesp.net", 2)[^1]
								.Split("/esoui/art/icons/", 2)[^1]
								.Replace(".dds", string.Empty, StringComparison.OrdinalIgnoreCase)
								.Replace(".png", string.Empty, StringComparison.OrdinalIgnoreCase)
								.Trim();
							list.Add(name);
						}

						text = text.Remove(match.Index, match.Length);
						match = OriginalFileFinder.Match(text);
					}

					foreach (var icon in foundIcons)
					{
						list.Add(icon);
					}

					var newText = text.TrimStart() + this.GetUsageList(IsBook(page), list);
					ReplaceNormal(parser, summaryIndex, newText);
				}

				this.ReplaceLicense(parser);
				page.Text = parser.GetText();
			}
		}

		private void ReplaceLicense(ContextualParser parsedPage)
		{
			if (parsedPage.Nodes.Find<SiteTemplateNode>(template => this.licenseTemplates.Contains(template.TitleValue)) is SiteTemplateNode license)
			{
				license.Title.Clear();
				license.Title.AddText("Zenimage");
				license.Parameters.Clear();
			}
		}
		#endregion

		#region Private Sealed Classes
		private sealed class ItemInfo
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