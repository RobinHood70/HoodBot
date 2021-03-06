namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;
	using RobinHood70.WikiCommon.Parser.Basic;
	using static RobinHood70.CommonCode.Globals;

	public class EsoMatchIcons : EditJob
	{
		#region Constants
		private const string MissingFileCategory = "Online-Icons-Missing Original File";
		#endregion

		#region Static Fields
		private static readonly DateTime LastRun = new DateTime(2020, 9, 25);
		private static readonly string WikiImageFolder = UespSite.GetBotDataFolder(@"WikiImages\"); // Files in this folder come from http://esofiles.uesp.net/update-<whatever>/icons.zip
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
			this.Pages.LoadOptions = new PageLoadOptions(PageModules.Info | PageModules.Revisions | PageModules.FileInfo);
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

			this.StatusWriteLine("Loading Pages");
			this.Pages.PageLoaded += this.Pages_PageLoaded;
			this.Pages.GetNamespace(MediaWikiNamespaces.File, Filter.Exclude, "ON-icon-achievement-");
			this.Pages.PageLoaded -= this.Pages_PageLoaded;
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
		private static bool IsBook(PageParts parts)
		{
			var isBook = false;
			foreach (var link in parts.Categories)
			{
				if (link.PageNameEquals("Online-Icons-Books") ||
					(link.PageNameEquals("Online-Icons-Quest Items") && parts.Page.PageName.Contains("Book", StringComparison.Ordinal)) ||
					link.PageNameEquals("ON-icon-minor adornment-Necklace.png") ||
					link.PageNameEquals("ON-icon-minor adornment-Ring.png"))
				{
					isBook = true;
					break;
				}
			}

			return isBook;
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

					list.Add(file[iconFolder.Length..].Replace(".png", string.Empty, StringComparison.OrdinalIgnoreCase).Replace('\\', '/'));
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

		private void GetUsageList(PageParts parts)
		{
			var isBook = IsBook(parts);
			foreach (var name in parts.OriginalFiles)
			{
				if (isBook || !this.allItems.TryGetValue(name, out var iconItems) || iconItems.Count <= 0)
				{
					parts.UnmatchedNames.Add(name);
				}
				else if (!isBook)
				{
					var sorted = new SortedSet<string>(StringComparer.Ordinal);
					foreach (var item in iconItems)
					{
						var text = item.Type switch
						{
							"achievements" => "Achievement: " + item.ItemName,
							"book" => "Book: " + item.ItemName,
							"collectibles" => $"Collectible: {{{{Item Link|{item.ItemName}|collectid={item.Id.ToStringInvariant()}}}}}",
							"minedItemSummary" => $"Mined Item: {{{{Item Link|{item.ItemName}|itemid={item.Id.ToStringInvariant()}}}}}",
							"questItem" => $"Quest Item: " + item.ItemName,
							_ => null,
						};

						if (text != null && !sorted.Contains(text))
						{
							sorted.Add(text);
						}
					}

					parts.UsedFor.Add(name, sorted);
				}
			}
		}

		private void LoadQueryData(KeyValuePair<string, string> query)
		{
			foreach (var row in Database.RunQuery(EsoGeneral.EsoLogConnectionString, query.Value))
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
			if (page is FilePage filePage && filePage.LatestFileRevision is FileRevision latestRevision)
			{
				this.allIcons.TryGetValue(latestRevision.Sha1 ?? throw PropertyNull(nameof(latestRevision), nameof(latestRevision.Sha1)), out var foundIcons);
				var parser = new ContextualParser(page);
				this.ReplaceLicense(parser);
				var parts = new PageParts(filePage);
				if (foundIcons == null || foundIcons.Count == 0)
				{
					if (parts.OriginalFiles.Count > 0)
					{
						this.WriteLine($"* [[{page.FullPageName}]] used to be identified, but no longer is. It may have been removed from the game.");
						return;
					}

					if (latestRevision.Timestamp > LastRun)
					{
						parts.Categories.Add(new SiteLink(page.Site, MissingFileCategory));
					}
				}
				else
				{
					foreach (var icon in foundIcons)
					{
						parts.OriginalFiles.Add(icon);
					}

					this.GetUsageList(parts);
				}

				page.Text = parts.ToText();
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

		private sealed class PageParts
		{
			#region Constructors
			public PageParts(FilePage page)
			{
				this.Page = page;
				var parser = new ContextualParser(page);
				for (var i = 0; i < parser.Nodes.Count; i++)
				{
					if (parser.Nodes[i] is SiteLinkNode link && link.TitleValue.Namespace == MediaWikiNamespaces.Category)
					{
						this.Categories.Add(SiteLink.FromLinkNode(page.Site, link));
						parser.Nodes.RemoveAt(i);
					}
				}

				var summaryIndex = parser.IndexOfHeader("Summary") + 1;
				this.PreSummary = summaryIndex == 0
					? (new IWikiNode[] { parser.Nodes.Factory.HeaderNodeFromParts(2, " Summary ") })
					: (ICollection<IWikiNode>)new List<IWikiNode>(parser.Nodes.GetRange(0, summaryIndex));

				var summaryEnd = parser.Nodes.FindIndex<HeaderNode>(summaryIndex);
				if (summaryEnd == -1)
				{
					summaryEnd = parser.Nodes.Count;
				}
				else
				{
					this.PostSummary = new List<IWikiNode>(parser.Nodes.GetRange(summaryEnd, parser.Nodes.Count - summaryEnd));
				}

				var preText = true;
				var summaryLines = WikiTextVisitor.Raw(parser.Nodes.GetRange(summaryIndex, summaryEnd - summaryIndex)).Split(TextArrays.NewLineChars);
				foreach (var line in summaryLines)
				{
					var fromWeb = line.Split("://esoicons.uesp.net/esoui/art/icons/", StringSplitOptions.RemoveEmptyEntries);
					if (fromWeb.Length > 1)
					{
						for (var i = 1; i < fromWeb.Length; i += 2)
						{
							var name = fromWeb[i].Split(TextArrays.Space, 2)[0];
							name = name
								.Replace(".dds", string.Empty, StringComparison.Ordinal)
								.Replace(".png", string.Empty, StringComparison.Ordinal)
								.Replace("<br>", string.Empty, StringComparison.OrdinalIgnoreCase);
							this.OriginalFiles.Add(name);
						}
					}
					else
					{
						var split = line
							.Trim()
							.TrimStart(TextArrays.Colon)
							.TrimStart()
							.Split(TextArrays.Colon, 2);
						switch (split[0].ToLowerInvariant())
						{
							case "original file":
							case "original filename":
								this.OriginalFiles.AddRange(split[1].TrimStart()
									.Replace("<br>", string.Empty, StringComparison.OrdinalIgnoreCase)
									.Split(TextArrays.CommaSpace, StringSplitOptions.RemoveEmptyEntries));
								break;
							case "achievement":
							case "book":
							case "collectible":
							case "mined item":
							case "quest item":
							case "used for":
								preText = false;
								break;
							default:
								if (preText)
								{
									this.PreText += line.Trim();
								}
								else
								{
									this.PostText += line.Trim();
								}

								break;
						}
					}
				}
			}
			#endregion

			#region Public Properties
			public ICollection<SiteLink> Categories { get; } = new SortedSet<SiteLink>(SimpleTitleComparer.Instance);

			public ICollection<string> OriginalFiles { get; } = new SortedSet<string>(StringComparer.Ordinal);

			public FilePage Page { get; }

			public ICollection<IWikiNode>? PostSummary { get; }

			public string PostText { get; } = string.Empty;

			public ICollection<IWikiNode>? PreSummary { get; }

			public string PreText { get; } = string.Empty;

			public ICollection<string> UnmatchedNames { get; } = new SortedSet<string>(StringComparer.Ordinal);

			public IDictionary<string, ICollection<string>> UsedFor { get; } = new SortedDictionary<string, ICollection<string>>(StringComparer.Ordinal);
			#endregion

			#region Public Methods
			public string ToText()
			{
				var sb = new StringBuilder()
					.Append(WikiTextVisitor.Raw(this.PreSummary).TrimEnd())
					.Append('\n');
				if (!string.IsNullOrEmpty(this.PreText))
				{
					sb
						.Append(this.PreText)
						.Append("\n\n");
				}

				if (this.UnmatchedNames.Count > 0)
				{
					DoubleSpace(sb);
					sb
						.Append("Original file: ")
						.AppendJoin(", ", this.UnmatchedNames)
						.Append('\n');
				}

				foreach (var itemList in this.UsedFor)
				{
					DoubleSpace(sb);
					sb
						.Append("Original file: ")
						.Append(itemList.Key)
						.Append("<br>\nUsed for:");
					if (itemList.Value.Count == 0)
					{
						sb.Append(" Nothing\n");
					}
					else
					{
						sb.Append('\n');
						foreach (var item in itemList.Value)
						{
							sb
								.Append(':')
								.Append(item)
								.Append('\n');
						}
					}
				}

				if (!string.IsNullOrEmpty(this.PostText))
				{
					DoubleSpace(sb);
					sb
						.Append(this.PostText)
						.Append('\n');
				}

				if (this.Categories.Count > 0)
				{
					DoubleSpace(sb);
					foreach (var cat in this.Categories)
					{
						sb
							.Append(cat)
							.Append('\n');
					}
				}

				if (this.PostSummary?.Count > 0)
				{
					sb.Append(WikiTextVisitor.Raw(this.PostSummary).TrimStart());
				}

				return sb.ToString().TrimEnd();

				static void DoubleSpace(StringBuilder sb)
				{
					if (!"\n=".Contains(sb[^2], StringComparison.Ordinal))
					{
						sb.Append('\n');
					}
				}
			}
			#endregion
		}
		#endregion
	}
}