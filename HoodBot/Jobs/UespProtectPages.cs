namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using RobinHood70.CommonCode;

using RobinHood70.HoodBot.Uesp;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

internal sealed class UespProtectPages : EditJob
{
	#region Private Constants
	private const string ProtectionTemplateName = "Protection";
	#endregion

	#region Static Fields
	private static readonly PageProtection GamespaceProtection = new(
		"Gamespace Pages",
		ProtectionLevel.Semi,
		ProtectionLevel.Full,
		AddStandardProtection,
		string.Empty,
		string.Empty,
		false,
		"main gamespace or similar page");

	private static readonly Dictionary<ProtectionLevel, string> ProtectionString = new()
	{
		[ProtectionLevel.None] = "None",
		[ProtectionLevel.Semi] = "Semi",
		[ProtectionLevel.Full] = "Full",
	};

	private static readonly Regex Dates = new("[0-9]{1,2} [a-zA-Z]+ 20[0-9]{2} ", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
	#endregion

	#region Fields
	private readonly IDictionary<Title, PageProtection> pageProtections = new SortedDictionary<Title, PageProtection>();
	private readonly List<ProtectionInfo> searchList =
	[
		new ProtectionInfo([MediaWikiNamespaces.Project], @"\AJavascript/.*?\.js", new PageProtection(
			"Javascript",
			ProtectionLevel.Full,
			ProtectionLevel.Full,
			AddJavascriptProtection,
			string.Empty,
			string.Empty,
			false,
			"highly vulnerable to vandalism")),
		new ProtectionInfo([MediaWikiNamespaces.Project], @"\ASite Support/", new PageProtection(
			"Site Support",
			ProtectionLevel.Full,
			ProtectionLevel.Full,
			null,
			"{{Site Support Trail}}",
			string.Empty,
			false,
			"site financial data")),
		new ProtectionInfo([MediaWikiNamespaces.Project], @"\A(Dev|Upgrade) History/", new PageProtection(
			"Upgrade History Archives",
			ProtectionLevel.Semi,
			ProtectionLevel.Semi,
			AddStandardProtection,
			"{{Archive Header|none|date=<date>}}",
			"{{Archive Footer}}",
			false,
			"archive protection policy")),
		new ProtectionInfo([MediaWikiNamespaces.Project], @"\A(Administrator Noticeboard|Community Portal)/Archives\Z", new PageProtection(
			"Archive Index",
			ProtectionLevel.Semi,
			ProtectionLevel.Semi,
			AddStandardProtection,
			string.Empty,
			string.Empty,
			false,
			"archive protection policy")),
		new ProtectionInfo([MediaWikiNamespaces.Project], @"\A(Administrator Noticeboard|Community Portal)/Archive [0-9]+\Z", new PageProtection(
			"AN/CP Archives",
			ProtectionLevel.Semi,
			ProtectionLevel.Semi,
			AddStandardProtection,
			"{{Archive Header|none|date=<date>}}",
			"{{Archive Footer}}",
			false,
			"archive protection policy")),
		new ProtectionInfo([MediaWikiNamespaces.Project], @"\AAdministrator Noticeboard/Vandalism\Z", new PageProtection(
			"Non-archive AN Subpages",
			ProtectionLevel.None,
			ProtectionLevel.None,
			null,
			string.Empty,
			string.Empty,
			true,
			"page should not be protected")),
		new ProtectionInfo([MediaWikiNamespaces.Project], @"\AAdministrator Noticeboard/Block Notifications\Z", new PageProtection(
			"Non-archive AN Subpages",
			ProtectionLevel.Semi,
			ProtectionLevel.Semi,
			null,
			string.Empty,
			string.Empty,
			true,
			"archive protection policy")),
		new ProtectionInfo([MediaWikiNamespaces.Project], @"\ACommunity Portal/Templates\Z", new PageProtection(
			"Non-archive CP Subpages",
			ProtectionLevel.None,
			ProtectionLevel.None,
			null,
			string.Empty,
			string.Empty,
			true,
			"page should not be protected")),
		new ProtectionInfo([MediaWikiNamespaces.Project], @"\A(Archive|Administrator Noticeboard|Community Portal)/", new PageProtection(
			"Site Discussions",
			ProtectionLevel.Semi,
			ProtectionLevel.Semi,
			AddStandardProtection,
			"{{Archive Header|none|date=<date>}}",
			string.Empty,
			true,
			"archive protection policy")),
		new ProtectionInfo([MediaWikiNamespaces.Project], @"\ADeletion Review/", new PageProtection(
			"Deletion Review",
			ProtectionLevel.Semi,
			ProtectionLevel.Semi,
			AddStandardProtection,
			"{{Archive Header|source=UESPWiki:Deletion Review|Deletion Review|Deletion Review Needs Attention|date=<date>}}",
			string.Empty,
			false,
			"archive protection policy")),
		new ProtectionInfo([MediaWikiNamespaces.Project], @"\AMessages/", new PageProtection(
			"Messages",
			ProtectionLevel.Semi,
			ProtectionLevel.Semi,
			null,
			string.Empty,
			string.Empty,
			false,
			"highly vulnerable to vandalism")),
		new ProtectionInfo([MediaWikiNamespaces.Project], @"\AReference Desk/", new PageProtection(
			"Reference Desk",
			ProtectionLevel.Semi,
			ProtectionLevel.Semi,
			AddStandardProtection,
			"{{Archive Header|none|date=<date>}}",
			"{{Archive Footer}}",
			false,
			"archive protection policy")),
	];
	#endregion

	#region Constructors
	[JobInfo("Protect UESP Pages")]
	public UespProtectPages(JobManager jobManager)
		: base(jobManager)
	{
		this.Pages.SetLimitations(LimitationType.None);
		List<int> talkSpaces = [];
		foreach (var ns in this.Site.Namespaces)
		{
			if (ns.Id == MediaWikiNamespaces.Project || (ns.IsTalkSpace && ns.Id != MediaWikiNamespaces.UserTalk))
			{
				talkSpaces.Add(ns.Id);
			}
		}

		this.searchList.Add(new ProtectionInfo(talkSpaces, @"/Arc.*?[0-9]+\Z", new PageProtection(
			"Numbered Archives",
			ProtectionLevel.Semi,
			ProtectionLevel.Semi,
			AddStandardProtection,
			"{{Archive Header|none|date=<date>}}",
			"{{Archive Footer}}",
			false,
			"archive protection policy")));
		this.searchList.Add(new ProtectionInfo(talkSpaces, "/Arc", new PageProtection(
			"Unnumbered Archives",
			ProtectionLevel.Semi,
			ProtectionLevel.Semi,
			AddStandardProtection,
			"{{Archive Header|none|date=<date>}}",
			string.Empty,
			true,
			"archive protection policy")));
	}
	#endregion

	#region Private Delegates
	private delegate int ProtectionTemplateFunc(SiteParser parser, PageProtection protection, int insertPos);
	#endregion

	#region Public Override Properties
	public override string LogName => "Protect Pages";
	#endregion

	#region Protected Override Methods
	protected override void AfterLoadPages()
	{
		this.WriteLine("== Page Protection Mismatches ==");
		this.WriteLine("{| class=\"wikitable sortable\"");
		this.WriteLine("! Group");
		this.WriteLine("! Page");
		this.WriteLine("! Original Protection");
		this.WriteLine("! New Protection");
		this.WriteLine("! Reason");

		foreach (var page in this.Pages)
		{
			var protection = this.pageProtections[page.Title];
			if (page.Exists &&
				(!protection.FriendlyName.OrdinalEquals("Deletion Review") ||
				page.StartTimestamp?.AddDays(30) < DateTime.Now))
			{
				this.WriteLine("|-");
				this.WriteLine("| " + protection.FriendlyName);
				this.WriteLine("| " + SiteLink.ToText(page));
				this.WriteLine("| " + CombinedProtectionString(
					ProtectionFromPage(page, "edit"),
					ProtectionFromPage(page, "move")));
				this.WriteLine("| " + CombinedProtectionString(
					protection.EditProtection,
					protection.MoveProtection));
				this.WriteLine("| " + protection.Reason.UpperFirst(this.Site.Culture));
			}
		}

		this.WriteLine("|}");
	}

	protected override void BeforeLoadPages()
	{
		this.StatusWriteLine("Loading Page Names");
		var namespacesToLoad = this.NamespacesInSearchList();
		var titlesToProtect = this.LoadPageNames(namespacesToLoad);
		this.StatusWriteLine("Loading Current Protection Levels");
		var pageLoadOptions = new PageLoadOptions(PageModules.Info, true) { InfoGetProtection = true };
		var currentProtectionPages = PageCollection.Unlimited(this.Site, pageLoadOptions);
		currentProtectionPages.GetTitles(titlesToProtect.ToTitles());
		currentProtectionPages.RemoveExists(false);
		foreach (var protectedTitle in titlesToProtect)
		{
			var protection = protectedTitle.Protection;
			if (currentProtectionPages.TryGetValue(protectedTitle.Title, out var page))
			{
				var editProtection = ProtectionFromPage(page, "edit");
				var moveProtection = ProtectionFromPage(page, "move");
				if (protection.EditProtection != editProtection || protection.MoveProtection != moveProtection)
				{
					this.pageProtections.TryAdd(page.Title, protection);
				}
			}
		}

		if (this.pageProtections.Count == 0)
		{
			this.Logger = null; // Temporary kludge to avoid logging. Change BeforeMain to boolean to check if we should proceed.
			this.StatusWriteLine("No pages needed to be changed.");
		}
	}

	protected override string GetEditSummary(Page page) => "Add/update templates";

	protected override void PageLoaded(Page page) => this.UpdatePage(page);

	protected override void Main()
	{
		if (this.Pages.Count == 0)
		{
			this.StatusWriteLine("No pages to save!");
			return;
		}

		this.StatusWriteLine(string.Format(this.Site.Culture, "Protecting {0} pages", this.Pages.Count));
		this.Pages.Sort();
		this.ProgressMaximum = this.Pages.Count;
		this.Progress = 0;
		foreach (var page in this.Pages)
		{
			var protection = this.pageProtections[page.Title];
			page.Title.Protect(protection.Reason, protection.EditProtection, protection.MoveProtection, DateTime.MaxValue);

			if (page.TextModified)
			{
				this.SavePage(page);
			}

			this.Progress++;
		}
	}

	protected override void LoadPages()
	{
		var titles = new TitleCollection(this.Site, this.pageProtections.Keys);
		this.Pages.GetTitles(titles);
	}
	#endregion

	#region Private Static Methods

	private static void AddFooter(SiteParser parser, PageProtection protection)
	{
		var footer = protection.Footer;
		var footerTemplate = (SiteTemplateNode)parser.Factory.TemplateNodeFromWikiText(footer);
		if (parser.FindSiteTemplate(footerTemplate.Title.PageName) is SiteTemplateNode existing)
		{
			existing.TitleNodes.Clear();
			existing.TitleNodes.AddRange(footerTemplate.TitleNodes);
		}
		else
		{
			parser.AddText("\n\n");
			parser.AddRange(footerTemplate);
		}
	}

	private static int AddHeader(SiteParser parser, PageProtection protection, int insertPos)
	{
		var nodes = parser;
		var header = protection.Header;
		var replaceDate = string.Empty;
		if (header.Contains("<date>", StringComparison.Ordinal))
		{
			replaceDate = GetDate(parser);
			header = header.Replace("<date>", replaceDate, StringComparison.Ordinal);
		}

		var headerTemplate = (SiteTemplateNode)nodes.Factory.TemplateNodeFromWikiText(header);
		var index = nodes.FindIndex<SiteTemplateNode>(node => node.Title == headerTemplate.Title);
		if (index != -1)
		{
			var existing = (SiteTemplateNode)nodes[index];
			existing.TitleNodes.Clear();
			existing.TitleNodes.AddRange(headerTemplate.TitleNodes);
			nodes.RemoveAt(index);
			if (index < insertPos)
			{
				insertPos--;
			}

			var title = parser.Page.Title;
			index = existing.FindIndex("source");
			if (index != -1)
			{
				var sourceTitle = TitleFactory.FromUnvalidated(parser.Site, existing.Parameters[index].GetValue()).ToTitle();
				if (sourceTitle.Namespace == title.Namespace && sourceTitle.PageNameEquals(title.BasePageName()))
				{
					existing.Parameters.RemoveAt(index);
				}
			}

			if (replaceDate.Length > 0 && existing.Find("date") == null)
			{
				existing.Add("date", replaceDate);
			}

			headerTemplate = existing;
		}

		var needsNewLine = nodes[insertPos] is IHeaderNode;
		nodes.Insert(insertPos, headerTemplate);
		if (needsNewLine)
		{
			nodes.Insert(insertPos + 1, nodes.Factory.TextNode("\n"));
		}

		return insertPos + 1;
	}

	private static int AddJavascriptProtection(SiteParser parser, PageProtection protection, int insertPos)
	{
		ITemplateNode protectionTemplate;
		var currentPos = parser.FindIndex<SiteTemplateNode>(node => node.Title.PageNameEquals(ProtectionTemplateName));
		if (currentPos != -1)
		{
			if (currentPos > 0 && parser[currentPos - 1] is ITextNode textNode && textNode.Text.Equals("// ", StringComparison.Ordinal))
			{
				protectionTemplate = (ITemplateNode)parser[currentPos];
				protectionTemplate.TitleNodes.Clear();
				protectionTemplate.TitleNodes.AddText(ProtectionTemplateName);
				protectionTemplate.Remove("edit");
				protectionTemplate.Remove("move");
				protectionTemplate.Remove("2");
				protectionTemplate.Remove("1");

				// Remove existing template and parameter values, then put them where we want them.
				parser.RemoveRange(0, 2);
			}
			else
			{
				throw new InvalidOperationException("Javascript protection is malformed. Please fix by hand.");
			}
		}
		else
		{
			protectionTemplate = parser.Factory.TemplateNodeFromParts(ProtectionTemplateName);
		}

		protectionTemplate.Add(ProtectionString[protection.EditProtection].ToLowerInvariant());
		if (protection.MoveProtection != protection.EditProtection)
		{
			protectionTemplate.Add(ProtectionString[protection.MoveProtection].ToLowerInvariant());
		}

		var newNodes = new IWikiNode[]
		{
			parser.Factory.TextNode("// "),
			protectionTemplate
		};
		parser.InsertRange(insertPos, newNodes);

		return insertPos + 2;
	}

	private static int AddStandardProtection(SiteParser parser, PageProtection protection, int insertPos)
	{
		insertPos = RemoveProtectionTemplate(parser, insertPos);
		var editWord = ProtectionString[protection.EditProtection].ToLowerInvariant();
		var moveWord = ProtectionString[protection.MoveProtection].ToLowerInvariant();
		return InsertStandardProtectionTemplate(parser, protection, insertPos, editWord, moveWord);
	}

	private static string CombinedProtectionString(ProtectionLevel editProtection, ProtectionLevel moveProtection) => editProtection == moveProtection ? ProtectionString[editProtection] : $"Edit={ProtectionString[editProtection]}, Move={ProtectionString[moveProtection]}";

	private static string GetDate(SiteParser parser)
	{
		var minDate = DateTime.MaxValue;
		foreach (var node in parser.FindAll<ITextNode>())
		{
			foreach (var match in (IEnumerable<Match>)Dates.Matches(node.Text))
			{
				if (DateTime.TryParse(match.ToString(), parser.Site.Culture, System.Globalization.DateTimeStyles.AssumeUniversal, out var testDate) && testDate < minDate)
				{
					minDate = testDate;
				}
			}
		}

		return minDate < DateTime.MaxValue
			? minDate.ToString("yyyy MMMM", parser.Site.Culture)
			: string.Empty;
	}

	private static int InsertStandardProtectionTemplate(SiteParser parser, PageProtection protection, int insertPos, string editWord, string moveWord)
	{
		var protectionTemplate = parser.Factory.TemplateNodeFromParts(ProtectionTemplateName);
		if (protection.EditProtection != ProtectionLevel.None || protection.MoveProtection != ProtectionLevel.None)
		{
			protectionTemplate.Add(editWord);
			if (protection.MoveProtection != protection.EditProtection)
			{
				protectionTemplate.Add(moveWord);
			}
		}

		parser.Insert(insertPos, protectionTemplate);
		return insertPos + 1;
	}

	private static ProtectionLevel ProtectionFromPage(Page protTitle, string protectionType) =>
		protTitle.Protections.TryGetValue(protectionType, out var protection)
			? protection.Level switch
			{
				"sysop" => ProtectionLevel.Full,
				"autoconfirmed" => ProtectionLevel.Semi,
				_ => ProtectionLevel.None
			}
			: ProtectionLevel.None;

	private static int RemoveProtectionTemplate(SiteParser parser, int insertPos)
	{
		var currentPos = parser.FindIndex<SiteTemplateNode>(node => node.Title.PageNameEquals(ProtectionTemplateName));
		if (currentPos != -1)
		{
			var existing = (SiteTemplateNode)parser[currentPos];
			existing.TitleNodes.Clear();
			existing.TitleNodes.AddText(ProtectionTemplateName);
			existing.Remove("edit");
			existing.Remove("move");
			existing.Remove("2");
			existing.Remove("1");

			// Remove existing template and parameter values, then put them where we want them.
			parser.RemoveAt(currentPos);
			if (currentPos < insertPos)
			{
				insertPos--;
			}
		}

		return insertPos;
	}

	private void UpdatePage(Page page)
	{
		var protection = this.pageProtections[page.Title];
		var insertPos = 0;
		SiteParser parser = new(page);
		var nodes = parser;

		// Figure out where to put a new Protection tempalte: for redirects, immediately after the link with no noincludes added; for pages with noincludes, inside the noinclude if it's early in the page. For anything else, add noincludes if needed, then insert inside the noinclude.
		if (page.IsRedirect)
		{
			insertPos = nodes.FindIndex<ILinkNode>(0) + 1;
			nodes.InsertRange(insertPos, [nodes.Factory.TextNode("\n")]);
			insertPos++;
		}
		else if (protection.NoInclude && (
			(protection.AddProtectionTemplate != null && (protection.EditProtection != ProtectionLevel.None || protection.MoveProtection != ProtectionLevel.None))
			|| !string.IsNullOrEmpty(protection.Header)
			|| !string.IsNullOrEmpty(protection.Footer)))
		{
			while (insertPos < nodes.Count && !(nodes[insertPos] is IIgnoreNode ignoreNode && ignoreNode.Value.Equals("<noinclude>", StringComparison.OrdinalIgnoreCase)))
			{
				if (nodes[insertPos] is ITextNode)
				{
					break;
				}

				insertPos++;
			}

			// If we didn't bail out because it's an ITextNode, increment position to be after the IIgnoreNode.
			if (insertPos == nodes.Count || nodes[insertPos] is ITextNode)
			{
				insertPos = 1;
				var newNodes = new IWikiNode[]
				{
					nodes.Factory.IgnoreNode("<noinclude>"),
					nodes.Factory.IgnoreNode("</noinclude>"),
					nodes.Factory.TextNode("\n")
				};
				nodes.InsertRange(0, newNodes);
			}
			else
			{
				insertPos++;
			}
		}

		if (protection.AddProtectionTemplate != null)
		{
			insertPos = protection.AddProtectionTemplate(parser, protection, insertPos);
		}

		if (!string.IsNullOrEmpty(protection.Header))
		{
			insertPos = AddHeader(parser, protection, insertPos);
		}

		if (!string.IsNullOrEmpty(protection.Footer))
		{
			AddFooter(parser, protection);
		}

		// Check if we've pulled stuff out of an unwanted noinclude block.
		if (parser.Count > insertPos &&
			parser[insertPos] is IIgnoreNode open && open.Value.Equals("<noinclude>", StringComparison.OrdinalIgnoreCase) &&
			parser[insertPos + 1] is IIgnoreNode close && close.Value.Equals("</noinclude>", StringComparison.OrdinalIgnoreCase))
		{
			parser.RemoveRange(insertPos, 2);
		}

		parser.UpdatePage();
	}
	#endregion

	#region Private Methods
	private List<ProtectedTitle> LoadPageNames(HashSet<int> spacesToLoad)
	{
		List<ProtectedTitle> titlesToProtect = [];
		UespNamespaceList nsList = new(this.Site);
		foreach (var ns in nsList.Values)
		{
			if (ns.IsGamespace)
			{
				titlesToProtect.Add(new ProtectedTitle(ns.MainPage, GamespaceProtection));
			}
		}

		this.ProgressMaximum = spacesToLoad.Count;
		foreach (var ns in spacesToLoad)
		{
			TitleCollection titles = new(this.Site);
			titles.GetNamespace(ns);

			foreach (var title in titles)
			{
				foreach (var si in this.searchList)
				{
					if (si.Namespaces.Contains(title.Namespace.Id) && si.SearchPattern.IsMatch(title.PageName))
					{
						titlesToProtect.Add(new ProtectedTitle(title, si.PageProtection));
						break;
					}
				}
			}

			this.Progress++;
		}

		this.Progress = 0;
		return titlesToProtect;
	}

	private HashSet<int> NamespacesInSearchList()
	{
		HashSet<int> retval = [];
		foreach (var search in this.searchList)
		{
			foreach (var ns in search.Namespaces)
			{
				retval.Add(ns);
			}
		}

		return retval;
	}
	#endregion

	#region Private Classes
	private sealed class PageProtection(string friendlyName, ProtectionLevel editProtection, ProtectionLevel moveProtection, ProtectionTemplateFunc? addProtectionTemplate, string header, string footer, bool noInclude, string reason)
	{
		#region Public Properties
		public ProtectionTemplateFunc? AddProtectionTemplate { get; } = addProtectionTemplate;

		public ProtectionLevel EditProtection { get; } = editProtection;

		public string Footer { get; } = footer;

		public string FriendlyName { get; } = friendlyName;

		public string Header { get; } = header;

		public ProtectionLevel MoveProtection { get; } = moveProtection;

		public bool NoInclude { get; } = noInclude;

		public string Reason { get; } = reason;
		#endregion

		#region Public Override Methods
		public override string ToString() => this.FriendlyName;
		#endregion
	}

	private sealed class ProtectedTitle(Title title, PageProtection protection) : ITitle
	{
		#region Public Properties
		public PageProtection Protection { get; } = protection;

		public Title Title { get; } = title;
		#endregion
	}

	private sealed class ProtectionInfo(ICollection<int> namespaces, string regexPattern, PageProtection pageProtection)
	{
		#region Public Properties
		public ICollection<int> Namespaces { get; } = namespaces;

		public PageProtection PageProtection { get; } = pageProtection;

		public Regex SearchPattern { get; } = new Regex(regexPattern, RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
		#endregion
	}
	#endregion
}