﻿namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Design;
using RobinHood70.HoodBot.Jobs.JobModels;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;
using RobinHood70.WikiCommon.Parser.Basic;

public class EsoMatchIcons : EditJob
{
	// Images should be downloaded from latest version on https://esofiles.uesp.net/ in the icons.zip file before running this job.
	#region Constants
	private const string MissingFileCategory = "Online-Icons-Missing Original File";
	#endregion

	#region Static Fields
	private static readonly DateTime LastRun = new(2020, 9, 25);
	#endregion

	#region Fields
	private readonly HashSet<Title> licenseTemplates = [];
	private readonly Dictionary<string, List<ItemInfo>> allItems = new(StringComparer.Ordinal);
	private Dictionary<string, HashSet<string>>? allIcons;
	#endregion

	#region Constructors
	[JobInfo("Match Icons", "ESO")]
	public EsoMatchIcons(JobManager jobManager)
		: base(jobManager)
	{
		this.Pages.Modules = PageModules.Info | PageModules.Revisions | PageModules.FileInfo;
		this.Pages.SetLimitations(LimitationType.OnlyAllow, MediaWikiNamespaces.File);
	}
	#endregion

	#region Public Override Properties
	public override string LogName => "Match ON-icons with log viewer";
	#endregion

	#region Protected Override Methods
	protected override void BeforeLoadPages()
	{
		this.StatusWriteLine("Getting database info");
		this.GetItems();

		this.StatusWriteLine("Calculating image checksums");
		this.allIcons = EsoSpace.GetIconChecksums();

		this.StatusWriteLine("Getting image info from wiki");
		this.GetLicenseTemplates();
	}

	protected override string GetEditSummary(Page page) => this.LogName;

	protected override void LoadPages() => this.Pages.GetNamespace(MediaWikiNamespaces.File, Filter.Exclude, "ON-icon-achievement-");

	protected override void PageLoaded(Page page)
	{
		if (this.allIcons is null)
		{
			throw new InvalidOperationException();
		}

		if (page is FilePage filePage && filePage.LatestFileRevision is FileRevision latestRevision)
		{
			Globals.ThrowIfNull(latestRevision.Sha1, nameof(latestRevision), nameof(latestRevision.Sha1));
			this.allIcons.TryGetValue(latestRevision.Sha1, out var foundIcons);
			SiteParser parser = new(page);
			FindLicense(parser); // TODO: This is a dummy line put in here to shut up the nag until I can come back to this.
			this.ReplaceLicense(parser);
			PageParts parts = new(filePage);
			if (foundIcons == null || foundIcons.Count == 0)
			{
				if (parts.OriginalFiles.Count > 0)
				{
					this.WriteLine($"* [[{page.Title.FullPageName()}]] used to be identified, but no longer is. It may have been removed from the game.");
					return;
				}

				if (latestRevision.Timestamp > LastRun)
				{
					SiteLink link = TitleFactory.FromValidated(page.Site[MediaWikiNamespaces.Category], MissingFileCategory);
					parts.Categories.Add(link);
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
	#endregion

	#region Private Static Methods
	private static (int Index, int End) FindLicense(SiteParser parser)
	{
		int summaryIndex;
		int summaryEnd;
		summaryIndex = parser.IndexOfHeader("Summary");
		if (summaryIndex == -1)
		{
			summaryIndex = parser.IndexOfHeader("{{int:filedesc}}");
		}

		summaryIndex++;
		summaryEnd = parser.IndexOf<HeaderNode>(summaryIndex);
		if (summaryEnd == -1)
		{
			summaryEnd = parser.Count;
		}

		return (summaryIndex, summaryEnd);
	}

	private static (int Index, int End) FindSummary(SiteParser parser)
	{
		int summaryIndex;
		int summaryEnd;
		summaryIndex = parser.IndexOfHeader("Summary");
		if (summaryIndex == -1)
		{
			summaryIndex = parser.IndexOfHeader("{{int:filedesc}}");
		}

		summaryIndex++;
		summaryEnd = parser.IndexOf<HeaderNode>(summaryIndex);
		if (summaryEnd == -1)
		{
			summaryEnd = parser.Count;
		}

		return (summaryIndex, summaryEnd);
	}

	private static bool IsBook(PageParts parts)
	{
		var isBook = false;
		foreach (var link in parts.Categories)
		{
			var title = link.Title;
			if (title.PageNameEquals("Online-Icons-Books") ||
				(title.PageNameEquals("Online-Icons-Quest Items") && parts.Page.Title.PageName.Contains("Book", StringComparison.Ordinal)) ||
				title.PageNameEquals("ON-icon-minor adornment-Necklace.png") ||
				title.PageNameEquals("ON-icon-minor adornment-Ring.png"))
			{
				isBook = true;
				break;
			}
		}

		return isBook;
	}

	private static List<SiteLink> ParseCatgories(Site site, SiteParser parser)
	{
		List<SiteLink> retval = [];
		for (var i = 0; i < parser.Count; i++)
		{
			if (parser[i] is ILinkNode link)
			{
				var siteLink = SiteLink.FromLinkNode(site, link);
				if (siteLink.Title.Namespace == MediaWikiNamespaces.Category)
				{
					retval.Add(siteLink);
					parser.RemoveAt(i);
				}
			}
		}

		return retval;
	}
	#endregion

	#region Private Methods
	private void GetItems()
	{
		Dictionary<string, string> queries = new(StringComparer.Ordinal)
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
		TitleCollection copyrightTemplates = new(this.Site);
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
			if (isBook || !this.allItems.TryGetValue(name, out var iconItems) || iconItems.Count == 0)
			{
				parts.UnmatchedNames.Add(name);
			}
			else if (!isBook)
			{
				SortedSet<string> sorted = new(StringComparer.Ordinal);
				foreach (var item in iconItems)
				{
					var text = item.Type switch
					{
						"achievements" => "Achievement: " + item.ItemName,
						"book" => "Book: " + item.ItemName,
						"collectibles" => $"Collectible: {{{{Item Link|{item.ItemName}|collectid={item.Id.ToStringInvariant()}}}}}",
						"minedItemSummary" => $"Mined Item: {{{{Item Link|{item.ItemName}|itemid={item.Id.ToStringInvariant()}}}}}",
						"questItem" => "Quest Item: " + item.ItemName,
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
		foreach (var item in Database.RunQuery(EsoLog.Connection, query.Value, row => new ItemInfo(row, query.Key)))
		{
			if (!this.allItems.TryGetValue(item.IconName, out var list))
			{
				list = new List<ItemInfo>(1);
				this.allItems.Add(item.IconName, list);
			}

			list.Add(item);
		}
	}

	private void ReplaceLicense(SiteParser parsedPage)
	{
		var found = false;
		var i = 0;
		while (i < parsedPage.Count)
		{
			if (parsedPage[i] is ITemplateNode template &&
				this.licenseTemplates.Contains(template.GetTitle(this.Site)))
			{
				if (found)
				{
					parsedPage.RemoveAt(i);
					i--;
				}
				else
				{
					template.TitleNodes.Clear();
					template.TitleNodes.AddText("Zenimage");
					template.Parameters.Clear();
					found = true;
				}
			}

			i++;
		}
	}
	#endregion

	#region Private Classes
	private sealed class ItemInfo
	{
		public ItemInfo(IDataRecord row, string type)
		{
			var idType = row.GetDataTypeName(0);
			var id = idType switch
			{
				"BIGINT" => (long)row["id"],
				"INT" => (int)row["id"],
				_ => throw new InvalidCastException(),
			};
			var iconName = EsoLog.ConvertEncoding((string)row["icon"]);
			this.IconName = iconName
				.Replace("/esoui/art/icons/", string.Empty, StringComparison.OrdinalIgnoreCase)
				.Replace(".dds", string.Empty, StringComparison.OrdinalIgnoreCase);
			this.Id = id;
			this.ItemName = EsoLog.ConvertEncoding((string)row["name"]);
			this.Type = type;
		}

		public string IconName { get; }

		public long Id { get; }

		public string ItemName { get; }

		public string Type { get; }
	}

	private sealed class PageParts
	{
		#region Constructors
		public PageParts(Page page)
		{
			this.Page = page;
			SiteParser parser = new(page);
			this.Categories.AddRange(ParseCatgories(page.Site, parser));

			(var index, var end) = FindSummary(parser);
			this.PreSummary = index == 0
				? (new IWikiNode[] { parser.Factory.HeaderNodeFromParts(2, " Summary ") })
				: new List<IWikiNode>(parser.GetRange(0, index));
			if (end < parser.Count)
			{
				this.PostSummary = [.. parser.GetRange(end, parser.Count - end)];
			}

			this.ParseSummary(parser, index, end);
		}
		#endregion

		#region Public Properties
		public ICollection<SiteLink> Categories { get; } = new SortedSet<SiteLink>(TitleComparer.Instance);

		public ICollection<string> OriginalFiles { get; } = new SortedSet<string>(StringComparer.Ordinal);

		public Page Page { get; }

		public List<IWikiNode>? PostSummary { get; }

		public string PostText { get; } = string.Empty;

		public ICollection<IWikiNode>? PreSummary { get; }

		public string PreText { get; } = string.Empty;

		public ICollection<string> UnmatchedNames { get; } = new SortedSet<string>(StringComparer.Ordinal);

		public SortedDictionary<string, ICollection<string>> UsedFor { get; } = new SortedDictionary<string, ICollection<string>>(StringComparer.Ordinal);
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

		#region Private Methods
		private void ParseSummary(SiteParser parser, int summaryIndex, int summaryEnd)
		{
			var isPreText = true;
			StringBuilder preText = new();
			StringBuilder postText = new();
			foreach (var line in WikiTextVisitor.Raw(parser.GetRange(summaryIndex, summaryEnd - summaryIndex)).Split(TextArrays.NewLineChars))
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
							isPreText = false;
							break;
						default:
							(isPreText ? preText : postText).Append(line);
							break;
					}
				}
			}
		}
		#endregion
	}
	#endregion
}