namespace RobinHood70.Robby
{
	// TODO: Review access rights project-wide.
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Net;
	using System.Runtime.CompilerServices;
	using System.Text;
	using System.Text.RegularExpressions;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;
	using static RobinHood70.Robby.Properties.Resources;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Describes the result of an attempted change to the site.</summary>
	public enum ChangeStatus
	{
		/// <summary>No change operation has been attempted.</summary>
		Unknown,

		/// <summary>The change to the wiki was successful.</summary>
		Success,

		/// <summary>The wiki reported that the method failed, either partly or completely, depending on the method.</summary>
		Failure,

		/// <summary>The change to the wiki was ignored due to AllowEditing being set to false.</summary>
		EditingDisabled,

		/// <summary>The change to the wiki was ignored because it would have no effect.</summary>
		NoEffect,

		/// <summary>During the appropriate event, a subscriber requested that the attempted change be cancelled.</summary>
		Cancelled,
	}

	/// <summary>Represents a single wiki site.</summary>
	/// <seealso cref="IMessageSource" />
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Sufficiently maintainable for now. Could conceivably split off the LoadX() methods if needed, I suppose.")]
	public class Site : IMessageSource
	{
		#region Internal Constants
		internal const string ImageVAlignName = "alignment";
		internal const string ImageAltName = "alt";
		internal const string ImageBorderName = "border";
		internal const string ImageClassName = "class";
		internal const string ImageCaptionName = "caption";
		internal const string ImageFormatName = "format";
		internal const string ImageLanguageName = "language";
		internal const string ImageLinkName = "link";
		internal const string ImageHAlignName = "location";
		internal const string ImagePageName = "page";
		internal const string ImageSizeName = "size";
		internal const string ImageUprightName = "upright";
		#endregion

		#region Private Constants
		private const SiteInfoProperties NeededSiteInfo =
			SiteInfoProperties.General |
			SiteInfoProperties.Namespaces |
			SiteInfoProperties.NamespaceAliases |
			SiteInfoProperties.MagicWords |
			SiteInfoProperties.InterwikiMap;
		#endregion

		#region Static Fields
		private static readonly string[] DefaultRedirect = { "#REDIRECT" };

		private static readonly Dictionary<string, string[]> ImageMagicWords = new Dictionary<string, string[]>()
		{
			[ImageVAlignName] = new[] { "img_baseline", "img_sub", "img_super", "img_top", "img_text_top", "img_middle", "img_bottom", "img_text_bottom" },
			[ImageAltName] = new[] { "img_alt" },
			[ImageBorderName] = new[] { "img_border" },
			[ImageClassName] = new[] { "img_class" },
			[ImageFormatName] = new[] { "img_framed", "img_frameless", "img_thumbnail", "img_manualthumb" },
			[ImageLanguageName] = new[] { "img_lang" },
			[ImageLinkName] = new[] { "img_link" },
			[ImageHAlignName] = new[] { "img_right", "img_left", "img_center", "img_none" },
			[ImagePageName] = new[] { "img_page" },
			[ImageSizeName] = new[] { "img_width" },
			[ImageUprightName] = new[] { "img_upright" },
		};
		#endregion

		#region Fields
		private readonly Dictionary<string, MagicWord> magicWords = new Dictionary<string, MagicWord>();
		private CultureInfo culture = CultureInfo.CurrentCulture;
		private HashSet<Title> disambiguationTemplates;
		private Regex redirectTargetFinder;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="Site"/> class.</summary>
		/// <param name="wiki">The <see cref="IWikiAbstractionLayer"/> to use. This controls whether the API is used or some other access method.</param>
		public Site(IWikiAbstractionLayer wiki)
		{
			ThrowNull(wiki, nameof(wiki));
			wiki.Initializing += this.AbstractionLayer_Initializing;
			wiki.Initialized += this.AbstractionLayer_Initialized;
			wiki.WarningOccurred += this.AbstractionLayer_WarningOccurred;
			this.AbstractionLayer = wiki;
		}
		#endregion

		#region Finalizer

		/// <summary>Finalizes an instance of the <see cref="Site"/> class.</summary>
		~Site()
		{
			this.AbstractionLayer.WarningOccurred -= this.AbstractionLayer_WarningOccurred;
			this.AbstractionLayer.Initialized -= this.AbstractionLayer_Initialized;
			this.AbstractionLayer.Initializing -= this.AbstractionLayer_Initializing;
		}
		#endregion

		#region Events

		/// <summary>Occurs when a change is about to be made to the wiki. Subscribers have the option to indicate if they need the change to be cancelled.</summary>
		public event StrongEventHandler<Site, ChangeArgs> Changing;

		/// <summary>Occurs after the PageTextChanging event when a page is about to be edited on the wiki.</summary>
		public event StrongEventHandler<Site, PagePreviewArgs> PagePreview;

		/// <summary>Occurs when a page is about to be edited on the wiki. Subscribers may make additional changes to the page, or indicate if they need the change to be cancelled.</summary>
		public event StrongEventHandler<Site, PageTextChangeArgs> PageTextChanging;

		/// <summary>Occurs when a warning should be sent to the user.</summary>
		public event StrongEventHandler<Site, WarningEventArgs> WarningOccurred;
		#endregion

		#region Public Static Properties

		/// <summary>Gets a static dictionary associating sites and users with the UserFunctions class that best fits them.</summary>
		/// <value>The user functions classes.</value>
		public static IDictionary<string, UserFunctionsFactory> UserFunctionsClasses { get; } = new Dictionary<string, UserFunctionsFactory>() { };
		#endregion

		#region Public Properties

		/// <summary>Gets the wiki abstraction layer.</summary>
		/// <value>The wiki abstraction layer.</value>
		public IWikiAbstractionLayer AbstractionLayer { get; }

		/// <summary>Gets the article path.</summary>
		/// <value>The article path, where <c>$1</c> should be replaced with the URL-encoded article title. </value>
		public string ArticlePath { get; private set; }

		/// <summary>Gets a value indicating whether the first letter of titles is case-sensitive.</summary>
		/// <value><c>true</c> if the first letter of titles is case-sensitive; otherwise, <c>false</c>.</value>
		public bool CaseSensitive { get; private set; }

		/// <summary>Gets or sets a CultureInfo object base the wiki's language and variant.</summary>
		/// <value>The culture of the wiki.</value>
		/// <remarks>Not all languages available in MediaWiki have direct equivalents in Windows. The bot will attempt to fall back to the more general language or variant when possible, but this property is left settable in the event that the choice made is unacceptable. If the culture cannot be determined, <see cref="CultureInfo.CurrentCulture"/> is used instead. Attempting to set the Culture to null will also result in CurrentCulture being used.</remarks>
		public CultureInfo Culture
		{
			get => this.culture;
			set
			{
				this.culture = value ?? CultureInfo.CurrentCulture;
				this.EqualityComparerInsensitive = StringComparer.Create(value, true);
			}
		}

		/// <summary>Gets or sets the default load options.</summary>
		/// <value>The default load options.</value>
		/// <remarks>If you need to detect disambiguations, you should consider setting this to include Properties for wikis using Disambiguator or Templates for those that aren't.</remarks>
		public PageLoadOptions DefaultLoadOptions { get; set; } = PageLoadOptions.Default;

		/// <summary>Gets the list of disambiguation templates on wikis that aren't using Disambiguator.</summary>
		/// <value>The disambiguation templates.</value>
		/// <remarks>This will be auto-populated on first use if not already set.</remarks>
		public IEnumerable<Title> DisambiguationTemplates => this.disambiguationTemplates ?? this.LoadDisambiguationTemplates();

		/// <summary>Gets a value indicating whether the Disambiguator extension is available.</summary>
		/// <value><c>true</c> if the Disambiguator extension is available; otherwise, <c>false</c>.</value>
		public bool DisambiguatorAvailable { get; private set; }

		/// <summary>Gets or sets a value indicating whether methods that would alter the wiki should be disabled.</summary>
		/// <value><c>true</c> if editing should be disabled; otherwise, <c>false</c>.</value>
		/// <remarks>If set to true, most methods will silently fail, and their return <see cref="ChangeStatus.EditingDisabled"/>. This is primarily intended for testing new bot jobs without risking any unintended edits.</remarks>
		public bool EditingEnabled { get; set; } = false;

		/// <summary>Gets or sets the EqualityComparer for case-insensitive comparison.</summary>
		/// <value>The case-insensitive EqualityComparer.</value>
		/// <remarks>This provides a central, consistent comparer for anything that is site-aware.</remarks>
		public IEqualityComparer<string> EqualityComparerInsensitive { get; protected set; }

		/// <summary>Gets the interwiki map.</summary>
		/// <value>The interwiki map.</value>
		public InterwikiMap InterwikiMap { get; private set; }

		/// <summary>Gets a list of current magic words on the wiki.</summary>
		/// <value>The magic words.</value>
		public IReadOnlyDictionary<string, MagicWord> MagicWords => this.magicWords;

		/// <summary>Gets the main page of the site.</summary>
		/// <value>The main page.</value>
		public Page MainPage { get; private set; }

		/// <summary>Gets the wiki name.</summary>
		/// <value>The name of the wiki.</value>
		public string Name { get; private set; }

		/// <summary>Gets the wiki namespaces.</summary>
		/// <value>the wiki namespaces.</value>
		public NamespaceCollection Namespaces { get; private set; }

		/// <summary>Gets or sets the page creator.</summary>
		/// <value>The page creator.</value>
		/// <remarks>A PageCreator is an abstract factory which serves as a bridge between customized PageItem types from WallE and the corresponding custom Page type for Robby.</remarks>
		public PageCreator PageCreator { get; set; } = PageCreator.Default;

		/// <summary>Gets the name of the server—typically, the base URL.</summary>
		/// <value>The name of the server.</value>
		public string ServerName { get; private set; }

		/// <summary>Gets the bot's user name.</summary>
		/// <value>The bot's user name.</value>
		public User User { get; private set; }

		/// <summary>Gets the UserFunctions object that handles site- and/or user-specific functions.</summary>
		/// <value>A UserFunctions class or derivative that handles site- and/or user-specific functions.</value>
		public UserFunctions UserFunctions { get; private set; }

		/// <summary>Gets the MediaWiki version of the wiki.</summary>
		/// <value>The MediaWiki version of the wiki.</value>
		public string Version { get; private set; }
		#endregion

		#region Internal Properties
		internal Dictionary<string, Regex> ImageParameterRegexes { get; } = new Dictionary<string, Regex>();
		#endregion

		#region Public Static Methods

		/// <summary>Registers a user functions class under all site and username combinations.</summary>
		/// <param name="sites">The sites to add. If using the same UserFunctions class across all sites the user is operating on, this should be set to null or an empty list.</param>
		/// <param name="users">The users to add. If using the same UserFunctions class for all users on all sites specifed in &lt;paramref name="sites"/&gt;, this should be set to null or an empty list.</param>
		/// <param name="factoryMethod">The factory method that creates the correct UserFunctions class for the combination of site(s) and user(s).</param>
		public static void RegisterUserFunctionsClass(IReadOnlyCollection<string> sites, IReadOnlyCollection<string> users, UserFunctionsFactory factoryMethod)
		{
			if (sites == null || sites.Count == 0)
			{
				sites = new List<string>() { string.Empty };
			}

			if (users == null || users.Count == 0)
			{
				users = new List<string>() { string.Empty };
			}

			foreach (var site in sites)
			{
				foreach (var user in users)
				{
					UserFunctionsClasses[string.Concat(site, '/', user)] = factoryMethod;
				}
			}
		}
		#endregion

		#region Public Methods

		/// <summary>Downloads a resource to a local file.</summary>
		/// <param name="resource">The location of the resource (typically, the a Uri path). This does <em>not</em> have to be located on the wiki.</param>
		/// <param name="fileName">Name of the file.</param>
		/// <remarks><paramref name="resource"/> is not a <see cref="Uri"/> in order to satisfy <see cref="IWikiAbstractionLayer"/>'s agnosticism. In practice, however, it will almost certainly always be one.</remarks>
		public void Download(string resource, string fileName) => this.Download(new DownloadInput(resource, fileName));

		/// <summary>Downloads the most recent version of a file from the wiki.</summary>
		/// <param name="pageName">Name of the page. You do not have to specify the File namespace, but you may if it's convenient.</param>
		/// <param name="fileName">Name of the file.</param>
		public void DownloadFile(string pageName, string fileName)
		{
			var fileTitle = new TitleCollection(this.Namespaces[MediaWikiNamespaces.File], pageName);
			var filePages = fileTitle.Load(PageModules.FileInfo);
			if (filePages.Count == 1 && filePages[0] is FilePage filePage)
			{
				filePage.Download(fileName);
			}
		}

		/// <summary>Gets the article path.</summary>
		/// <param name="articleName">Name of the article.</param>
		/// <returns>A full Uri to the article.</returns>
		public Uri GetArticlePath(string articleName) => this.GetArticlePath(articleName, null);

		/// <summary>Gets the article path.</summary>
		/// <param name="articleName">Name of the article.</param>
		/// <param name="fragment">The fragment to jump to. May be null.</param>
		/// <returns>A full Uri to the article.</returns>
		public virtual Uri GetArticlePath(string articleName, string fragment) => this.GetArticlePath(this.ArticlePath, articleName, fragment);

		/// <summary>Gets the redirect target from the page text.</summary>
		/// <param name="text">The text to parse.</param>
		/// <returns>A <see cref="TitleParts"/> object with the parsed redirect.</returns>
		public virtual TitleParts GetRedirectFromText(string text)
		{
			ThrowNull(text, nameof(text));
			if (this.redirectTargetFinder == null)
			{
				var list = new List<string>();
				var redirects = new HashSet<string>(this.MagicWords.TryGetValue("redirect", out var redirect) ? redirect.Aliases : DefaultRedirect);
				foreach (var redirWord in redirects)
				{
					list.Add(Regex.Escape(redirWord));
				}

				// Regex originally taken from WikitextContent.php --> '!^\s*:?\s*\[{2}(.*?)(?:\|.*?)?\]{2}\s*!'
				this.redirectTargetFinder = new Regex("^(" + string.Join("|", list) + @")\s*:?\s*\[\[(?<target>.*?)(\|.*?)?\]\]", redirect.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
			}

			var target = this.redirectTargetFinder.Match(text).Groups["target"];

			return target.Success ? new TitleParts(this, target.Value) : null;
		}

		/// <summary>Gets all active blocks.</summary>
		/// <returns>All active blocks.</returns>
		public IReadOnlyList<Block> LoadBlocks() => this.LoadBlocks(new BlocksInput());

		/// <summary>Gets active blocks filtered by the specified set of attributes.</summary>
		/// <param name="account">Filter account blocks.</param>
		/// <param name="ip">Filter IP blocks.</param>
		/// <param name="range">Filter range blocks.</param>
		/// <param name="temporary">Filter temporary blocks.</param>
		/// <returns>Active blocks filtered by the specified set of attributes.</returns>
		/// <remarks>Filters intersect with one another, so <c>account := Filter.Only, temporary := Filter.Only</c> gives all temporary account blocks, while <c>account := Filter.Only, temporary := Filter.Exclude</c> gives all permanent account blocks. Note: there is no checking for nonsensical filters, so filters such as <c>account := Filter.Only, ip := Filter.Only</c> will succeed but won't return any results. By contrast, <c>account := Filter.Exclude, ip := Filter.Exclude</c> returns the same results as <c>range := Filter.Only</c>.</remarks>
		public IReadOnlyList<Block> LoadBlocks(Filter account, Filter ip, Filter range, Filter temporary) => this.LoadBlocks(new BlocksInput() { FilterAccount = account, FilterIP = ip, FilterRange = range, FilterTemporary = temporary });

		/// <summary>Gets active blocks filtered by the date when they were placed or last updated.</summary>
		/// <param name="start">The start date.</param>
		/// <param name="end">The end date.</param>
		/// <returns>Active blocks filtered by the date when they were placed or last updated.</returns>
		public IReadOnlyList<Block> LoadBlocks(DateTime start, DateTime end) => this.LoadBlocks(new BlocksInput() { Start = start, End = end });

		/// <summary>Gets active blocks filtered by the date when they were placed or last update and the specified set of attributes.</summary>
		/// <param name="start">The start date.</param>
		/// <param name="end">The end date.</param>
		/// <param name="account">Filter account blocks.</param>
		/// <param name="ip">Filter IP blocks.</param>
		/// <param name="range">Filter range blocks.</param>
		/// <param name="temporary">Filter temporary blocks.</param>
		/// <returns>Active blocks filtered by the date when they were placed or last update and the specified set of attributes.</returns>
		/// <remarks>Filters intersect with one another, so <c>account := Filter.Only, temporary := Filter.Only</c> gives all temporary account blocks, while <c>account := Filter.Only, temporary := Filter.Exclude</c> gives all permanent account blocks. Note: there is no checking for nonsensical filters, so filters such as <c>account := Filter.Only, ip := Filter.Only</c> will succeed but won't return any results. By contrast, <c>account := Filter.Exclude, ip := Filter.Exclude</c> returns the same results as <c>range := Filter.Only</c>.</remarks>
		public IReadOnlyList<Block> LoadBlocks(DateTime start, DateTime end, Filter account, Filter ip, Filter range, Filter temporary) => this.LoadBlocks(new BlocksInput() { Start = start, End = end, FilterAccount = account, FilterIP = ip, FilterRange = range, FilterTemporary = temporary });

		/// <summary>Gets active blocks for the specified set of users.</summary>
		/// <param name="users">The users.</param>
		/// <returns>Active blocks for the specified set of users.</returns>
		public IReadOnlyList<Block> LoadBlocks(IEnumerable<string> users) => this.LoadBlocks(new BlocksInput(users));

		/// <summary>Gets active blocks for the specified IP address.</summary>
		/// <param name="ip">The IP address.</param>
		/// <returns>Active blocks for the specified IP address.</returns>
		public IReadOnlyList<Block> LoadBlocks(IPAddress ip) => this.LoadBlocks(new BlocksInput(ip));

		/// <summary>Gets a message from MediaWiki space.</summary>
		/// <param name="msg">The message.</param>
		/// <param name="arguments">Optional arguments to substitute into the message.</param>
		/// <returns>The text of the message.</returns>
		public string LoadMessage(string msg, params string[] arguments) => this.LoadMessage(msg, arguments as IEnumerable<string>);

		/// <summary>Gets a message from MediaWiki space.</summary>
		/// <param name="msg">The message.</param>
		/// <param name="arguments">Optional arguments to substitute into the message.</param>
		/// <returns>The text of the message.</returns>
		public string LoadMessage(string msg, IEnumerable<string> arguments)
		{
			var messages = this.LoadMessages(new[] { msg }, arguments);
			return messages[msg].Text;
		}

		/// <summary>Gets multiple messages from MediaWiki space.</summary>
		/// <param name="messages">The messages.</param>
		/// <param name="arguments">Optional arguments to substitute into the messages.</param>
		/// <returns>A read-only dictionary of the specified arguments with their associated Message objects.</returns>
		public IReadOnlyDictionary<string, MessagePage> LoadMessages(IEnumerable<string> messages, params string[] arguments) => this.LoadMessages(messages, arguments as IEnumerable<string>);

		/// <summary>Gets multiple messages from MediaWiki space.</summary>
		/// <param name="messages">The messages.</param>
		/// <param name="arguments">Optional arguments to substitute into the messages.</param>
		/// <returns>A read-only dictionary of the specified arguments with their associated Message objects.</returns>
		public IReadOnlyDictionary<string, MessagePage> LoadMessages(IEnumerable<string> messages, IEnumerable<string> arguments) => this.LoadMessages(new AllMessagesInput
		{
			Messages = messages,
			Arguments = arguments,
		});

		/// <summary>Gets all page property names in use on the wiki.</summary>
		/// <returns>All page property names on the wiki.</returns>
		public IReadOnlyList<string> LoadPagePropertyNames() => this.AbstractionLayer.PagePropertyNames(new PagePropertyNamesInput());

		/// <summary>This is a convenience method to quickly get the text of a single page.</summary>
		/// <param name="pageName">Name of the page.</param>
		/// <returns>The text of the page.</returns>
		public string LoadPageText(string pageName)
		{
			var titles = new TitleCollection(this, pageName);
			titles.SetNamespaceLimitations(null, LimitationType.None);
			var result = titles.Load();
			return result.Count == 1 ? result[0].Text : null;
		}

		/// <summary>Gets a message from MediaWiki space with any magic words and the like parsed into text.</summary>
		/// <param name="msg">The message.</param>
		/// <param name="arguments">Optional arguments to substitute into the message.</param>
		/// <returns>The text of the message.</returns>
		public string LoadParsedMessage(string msg, params string[] arguments) => this.LoadParsedMessage(msg, arguments as IEnumerable<string>, null);

		/// <summary>Gets a message from MediaWiki space with any magic words and the like parsed into text.</summary>
		/// <param name="msg">The message.</param>
		/// <param name="arguments">Optional arguments to substitute into the message.</param>
		/// <returns>The text of the message.</returns>
		public string LoadParsedMessage(string msg, IEnumerable<string> arguments) => this.LoadParsedMessage(msg, arguments, null);

		/// <summary>Gets a message from MediaWiki space with any magic words and the like parsed into text.</summary>
		/// <param name="msg">The message.</param>
		/// <param name="arguments">Optional arguments to substitute into the message.</param>
		/// <param name="title">The title to use for parsing.</param>
		/// <returns>The text of the message.</returns>
		public string LoadParsedMessage(string msg, IEnumerable<string> arguments, Title title)
		{
			var messages = this.LoadParsedMessages(new[] { msg }, arguments, title);
			return messages[msg].Text;
		}

		/// <summary>Gets multiple messages from MediaWiki space with any magic words and the like parsed into text.</summary>
		/// <param name="messages">The messages.</param>
		/// <param name="arguments">Optional arguments to substitute into the message.</param>
		/// <returns>A read-only dictionary of the specified arguments with their associated Message objects.</returns>
		public IReadOnlyDictionary<string, MessagePage> LoadParsedMessages(IEnumerable<string> messages, IEnumerable<string> arguments) => this.LoadParsedMessages(messages, arguments, null);

		/// <summary>Gets multiple messages from MediaWiki space with any magic words and the like parsed into text.</summary>
		/// <param name="messages">The messages.</param>
		/// <param name="arguments">Optional arguments to substitute into the message.</param>
		/// <param name="title">The title to use for parsing.</param>
		/// <returns>A read-only dictionary of the specified arguments with their associated Message objects.</returns>
		public IReadOnlyDictionary<string, MessagePage> LoadParsedMessages(IEnumerable<string> messages, IEnumerable<string> arguments, Title title) => this.LoadMessages(new AllMessagesInput
		{
			Messages = messages,
			Arguments = arguments,
			EnableParser = true,
			EnableParserTitle = title?.FullPageName,
		});

		/// <summary>Gets all recent changes.</summary>
		/// <returns>A read-only list of all recent changes.</returns>
		public IReadOnlyList<RecentChange> LoadRecentChanges() => this.LoadRecentChanges(new RecentChangesOptions());

		/// <summary>Gets recent changes within the specified namespaces.</summary>
		/// <param name="namespaces">The namespaces.</param>
		/// <returns>A read-only list of recent changes within the specified namespaces.</returns>
		public IReadOnlyList<RecentChange> LoadRecentChanges(IEnumerable<int> namespaces) => this.LoadRecentChanges(new RecentChangesOptions() { Namespaces = namespaces });

		/// <summary>Gets recent changes with the specified tag.</summary>
		/// <param name="tag">The tag.</param>
		/// <returns>A read-only list of recent changes with the specified tag.</returns>
		public IReadOnlyList<RecentChange> LoadRecentChanges(string tag) => this.LoadRecentChanges(new RecentChangesOptions() { Tag = tag });

		/// <summary>Gets recent changes of the specified types.</summary>
		/// <param name="types">The types to return.</param>
		/// <returns>A read-only list of recent changes of the specified types.</returns>
		public IReadOnlyList<RecentChange> LoadRecentChanges(RecentChangesTypes types) => this.LoadRecentChanges(new RecentChangesOptions() { Types = types });

		/// <summary>Gets recent changes filtered by a set of attributes.</summary>
		/// <param name="anonymous">Filter anonymous edits.</param>
		/// <param name="bots">Filter bot edits.</param>
		/// <param name="minor">Filter minor edits.</param>
		/// <param name="patrolled">Filter patrolled edits.</param>
		/// <param name="redirects">Filter redirects.</param>
		/// <returns>A read-only list of recent changes with the specified attributes.</returns>
		/// <remarks>Filters intersect with one another, so <c>bots := Filter.Only, minor := Filter.Only</c> gives all minor bot edits, while <c>bots := Filter.Only, minor := Filter.Exclude</c> gives all bot edits which are <em>not</em> minor. Note: there is no checking for nonsensical filters. On most wikis, for example, anonymous minor edits are oxymoronic and will produce no results, but since configuration changes or extensions might allow this combination, the request will be sent to the wiki regardless.</remarks>
		public IReadOnlyList<RecentChange> LoadRecentChanges(Filter anonymous, Filter bots, Filter minor, Filter patrolled, Filter redirects) => this.LoadRecentChanges(new RecentChangesOptions() { Anonymous = anonymous, Bots = bots, Minor = minor, Patrolled = patrolled, Redirects = redirects });

		/// <summary>Gets recent changes between two dates.</summary>
		/// <param name="start">The start date.</param>
		/// <param name="end">The end date.</param>
		/// <returns>A read-only list of recent changes between the specified dates.</returns>
		public IReadOnlyList<RecentChange> LoadRecentChanges(DateTime start, DateTime end) => this.LoadRecentChanges(new RecentChangesOptions() { Start = start, End = end });

		/// <summary>Gets recent changes before or after the specified date.</summary>
		/// <param name="start">The start date.</param>
		/// <param name="newer">if set to <c>true</c>, returns changes from the specified date and newer; otherwise, it returns changes from the specified date and older.</param>
		/// <returns>A read-only list of recent changes before or after the specified date.</returns>
		public IReadOnlyList<RecentChange> LoadRecentChanges(DateTime start, bool newer) => this.LoadRecentChanges(start, newer, 0);

		/// <summary>Gets the specified number of recent changes before or after the specified date.</summary>
		/// <param name="start">The start date.</param>
		/// <param name="newer">if set to <c>true</c>, returns changes from the specified date and newer; otherwise, it returns changes from the specified date and older.</param>
		/// <param name="count">The number of recent changes to get.</param>
		/// <returns>A read-only list of recent changes before or after the specified date.</returns>
		public IReadOnlyList<RecentChange> LoadRecentChanges(DateTime start, bool newer, int count) => this.LoadRecentChanges(new RecentChangesOptions() { Start = start, Newer = newer, Count = count });

		/// <summary>Gets recent changes from or excluding the specified user.</summary>
		/// <param name="user">The user.</param>
		/// <param name="exclude">if set to <c>true</c>, get all recent changes <em>except</em> those from the specified user.</param>
		/// <returns>A read-only list of recent changes from or excluding the specified user.</returns>
		public IReadOnlyList<RecentChange> LoadRecentChanges(string user, bool exclude) => this.LoadRecentChanges(new RecentChangesOptions() { User = user, ExcludeUser = exclude });

		/// <summary>Gets recent changes according to a customized set of filter options.</summary>
		/// <param name="options">The filter options to apply.</param>
		/// <returns>A read-only list of recent changes that conform to the options specified.</returns>
		public virtual IReadOnlyList<RecentChange> LoadRecentChanges(RecentChangesOptions options)
		{
			ThrowNull(options, nameof(options));
			return this.LoadRecentChanges(options.ToWallEInput);
		}

		/// <summary>Gets public information about the specified users.</summary>
		/// <param name="users">The users.</param>
		/// <returns>A list of <see cref="User"/> objects for the specified users.</returns>
		public IReadOnlyList<User> LoadUserInformation(params string[] users) => this.LoadUserInformation(users as IEnumerable<string>);

		/// <summary>Gets public information about the specified users.</summary>
		/// <param name="users">The users.</param>
		/// <returns>A list of <see cref="User"/> objects for the specified users.</returns>
		public IReadOnlyList<User> LoadUserInformation(IEnumerable<string> users) => this.LoadUserInformation(new UsersInput(users) { Properties = UsersProperties.All });

		/// <summary>Gets all users on the wiki.</summary>
		/// <param name="onlyActiveUsers">if set to <c>true</c>, only active users will be returned.</param>
		/// <param name="onlyUsersWithEdits">if set to <c>true</c>, only users with edits will be returned.</param>
		/// <returns>A list of users based on the specified criteria.</returns>
		public IReadOnlyList<User> LoadUsers(bool onlyActiveUsers, bool onlyUsersWithEdits) => this.LoadUsers(new AllUsersInput { ActiveUsersOnly = onlyActiveUsers, WithEditsOnly = onlyUsersWithEdits });

		/// <summary>Gets users whose names start with the specified prefix.</summary>
		/// <param name="onlyActiveUsers">if set to <c>true</c>, only active users will be returned.</param>
		/// <param name="onlyUsersWithEdits">if set to <c>true</c>, only users with edits will be returned.</param>
		/// <param name="prefix">The prefix.</param>
		/// <returns>A list of users based on the specified criteria.</returns>
		public IReadOnlyList<User> LoadUsers(bool onlyActiveUsers, bool onlyUsersWithEdits, string prefix) => this.LoadUsers(new AllUsersInput { ActiveUsersOnly = onlyActiveUsers, WithEditsOnly = onlyUsersWithEdits, Prefix = prefix });

		/// <summary>Gets users whose names fall within the specified range.</summary>
		/// <param name="onlyActiveUsers">if set to <c>true</c>, only active users will be returned.</param>
		/// <param name="onlyUsersWithEdits">if set to <c>true</c>, only users with edits will be returned.</param>
		/// <param name="from">The name to start at (inclusive). The name specified does not have to exist.</param>
		/// <param name="to">The name to stop at (inclusive). The name specified does not have to exist.</param>
		/// <returns>A list of users based on the specified criteria.</returns>
		public IReadOnlyList<User> LoadUsers(bool onlyActiveUsers, bool onlyUsersWithEdits, string from, string to) => this.LoadUsers(new AllUsersInput { ActiveUsersOnly = onlyActiveUsers, WithEditsOnly = onlyUsersWithEdits, From = from, To = to });

		/// <summary>Gets the users that belong to the specified groups.</summary>
		/// <param name="onlyActiveUsers">if set to <c>true</c>, only active users will be retrieved.</param>
		/// <param name="onlyUsersWithEdits">if set to <c>true</c>, only users with edits will be retrieved.</param>
		/// <param name="groups">The groups.</param>
		/// <returns>A list of <see cref="User"/> objects for users in the specified groups.</returns>
		public IReadOnlyList<User> LoadUsersInGroups(bool onlyActiveUsers, bool onlyUsersWithEdits, params string[] groups) => this.LoadUsersInGroups(onlyActiveUsers, onlyUsersWithEdits, groups as IEnumerable<string>);

		/// <summary>Gets users that belong to the specified groups.</summary>
		/// <param name="onlyActiveUsers">if set to <c>true</c>, only active users will be retrieved.</param>
		/// <param name="onlyUsersWithEdits">if set to <c>true</c>, only users with edits will be retrieved.</param>
		/// <param name="groups">The groups.</param>
		/// <returns>A list of <see cref="User"/> objects for users in the specified groups.</returns>
		public IReadOnlyList<User> LoadUsersInGroups(bool onlyActiveUsers, bool onlyUsersWithEdits, IEnumerable<string> groups) => this.LoadUsers(new AllUsersInput { ActiveUsersOnly = onlyActiveUsers, WithEditsOnly = onlyUsersWithEdits, Groups = groups });

		/// <summary>Gets users that have the specified rights.</summary>
		/// <param name="onlyActiveUsers">if set to <c>true</c>, only active users will be retrieved.</param>
		/// <param name="onlyUsersWithEdits">if set to <c>true</c>, only users with edits will be retrieved.</param>
		/// <param name="rights">The rights.</param>
		/// <returns>A list of <see cref="User"/> objects for users with the specified rights.</returns>
		public IReadOnlyList<User> LoadUsersWithRights(bool onlyActiveUsers, bool onlyUsersWithEdits, params string[] rights) => this.LoadUsersWithRights(onlyActiveUsers, onlyUsersWithEdits, rights as IEnumerable<string>);

		/// <summary>Gets users that have the specified rights.</summary>
		/// <param name="onlyActiveUsers">if set to <c>true</c>, only active users will be retrieved.</param>
		/// <param name="onlyUsersWithEdits">if set to <c>true</c>, only users with edits will be retrieved.</param>
		/// <param name="rights">The rights.</param>
		/// <returns>A list of <see cref="User"/> objects for users with the specified rights.</returns>
		public IReadOnlyList<User> LoadUsersWithRights(bool onlyActiveUsers, bool onlyUsersWithEdits, IEnumerable<string> rights) => this.LoadUsers(new AllUsersInput { ActiveUsersOnly = onlyActiveUsers, WithEditsOnly = onlyUsersWithEdits, Rights = rights });

		/// <summary>Logs the specified user into the wiki and loads necessary information for proper functioning of the class.</summary>
		/// <param name="userName">Name of the user. This can be null if you wish to edit anonymously.</param>
		/// <param name="password">The password.</param>
		/// <exception cref="UnauthorizedAccessException">Thrown if there was an error logging into the wiki (which typically denotes that the user had the wrong password or does not have permission to log in).</exception>
		/// <remarks>Even if you wish to edit anonymously, you <em>must</em> still log in by passing <see langword="null" /> for the <paramref name="userName" /> parameter.</remarks>
		public void Login(string userName, string password) => this.Login(new LoginInput(userName, password));

		/// <summary>Logs the specified user into the wiki and loads necessary information for proper functioning of the class.</summary>
		/// <param name="userName">Name of the user. This can be null if you wish to edit anonymously.</param>
		/// <param name="password">The password.</param>
		/// <param name="domain">The domain.</param>
		/// <exception cref="UnauthorizedAccessException">Thrown if there was an error logging into the wiki (which typically denotes that the user had the wrong password or does not have permission to log in).</exception>
		/// <remarks>Even if you wish to edit anonymously, you <em>must</em> still log in by passing <see langword="null" /> for the <paramref name="userName" /> parameter.</remarks>
		public void Login(string userName, string password, string domain) => this.Login(new LoginInput(userName, password) { Domain = domain });

		/// <summary>Patrols the specified Recent Changes ID.</summary>
		/// <param name="rcid">The Recent Change ID.</param>
		/// <returns>A value indicating the change status of the patrol.</returns>
		public ChangeStatus Patrol(long rcid) => this.PublishChange(
			this,
			new Dictionary<string, object>
			{
				[nameof(rcid)] = rcid,
			},
			() => this.Patrol(new PatrolInput(rcid)).Title == null ? ChangeStatus.Failure : ChangeStatus.Success);

		/// <summary>Patrols the specified revision ID.</summary>
		/// <param name="revid">The revision ID.</param>
		/// <returns>A value indicating the change status of the patrol.</returns>
		public ChangeStatus PatrolRevision(long revid) => this.PublishChange(
			this,
			new Dictionary<string, object>
			{
				[nameof(revid)] = revid,
			},
			() => this.Patrol(new PatrolInput(revid)).Title == null ? ChangeStatus.Failure : ChangeStatus.Success);

		/// <summary>Upload a file to the wiki.</summary>
		/// <param name="fileName">The full path and filename of the file to upload.</param>
		/// <param name="editSummary">The edit summary for the upload.</param>
		/// <remarks>The destination filename will be the same as the local filename.</remarks>
		/// <exception cref="ArgumentException">Path contains an invalid character.</exception>
		/// <returns>A value indicating the change status of the upload.</returns>
		public ChangeStatus Upload(string fileName, string editSummary) => this.Upload(fileName, null, editSummary, null);

		/// <summary>Upload a file to the wiki.</summary>
		/// <param name="fileName">The full path and filename of the file to upload.</param>
		/// <param name="destinationName">The bare name (i.e., do not include "File:") of the file to upload to on the wiki. Set to null to use the filename from the <paramref name="fileName" /> parameter.</param>
		/// <param name="editSummary">The edit summary for the upload.</param>
		/// <remarks>The destination filename will be the same as the local filename.</remarks>
		/// <exception cref="ArgumentException">Path contains an invalid character.</exception>
		/// <returns>A value indicating the change status of the upload.</returns>
		public ChangeStatus Upload(string fileName, string destinationName, string editSummary) => this.Upload(fileName, destinationName, editSummary, null);

		/// <summary>Upload a file to the wiki.</summary>
		/// <param name="fileName">The full path and filename of the file to upload.</param>
		/// <param name="destinationName">The bare name (i.e., do not include "File:") of the file to upload to on the wiki. Set to null to use the filename from the <paramref name="fileName" /> parameter.</param>
		/// <param name="editSummary">The edit summary for the upload.</param>
		/// <param name="pageText">Full page text for the File page. This should include the license, categories, and anything else required. Set to null to allow the wiki to generate the page text (normally just the <paramref name="editSummary" />).</param>
		/// <exception cref="ArgumentException">Path contains an invalid character.</exception>
		/// <returns>A value indicating the change status of the upload.</returns>
		public ChangeStatus Upload(string fileName, string destinationName, string editSummary, string pageText) => this.PublishChange(
			this,
			new Dictionary<string, object>
			{
				[nameof(fileName)] = fileName,
				[nameof(destinationName)] = destinationName,
				[nameof(editSummary)] = editSummary,
				[nameof(pageText)] = pageText,
			},
			() =>
			{
				var checkedName = Path.GetFileName(fileName); // Always access this, even if we don't need it, as a means of checking validity.
				if (string.IsNullOrWhiteSpace(destinationName))
				{
					destinationName = checkedName;
				}

				using (var upload = new FileStream(checkedName, FileMode.Open))
				{
					var uploadInput = new UploadInput(destinationName, upload)
					{
						IgnoreWarnings = true,
						Comment = editSummary,
					};

					if (pageText != null)
					{
						uploadInput.Text = pageText;
					}

					return this.Upload(uploadInput) ? ChangeStatus.Success : ChangeStatus.Failure;
				}
			});
		#endregion

		#region Public Virtual Methods

		/// <summary>Clears the bot's "has message" flag.</summary>
		/// <returns><c>true</c> if the flag was successfully cleared; otherwise, <c>false</c>.</returns>
		public virtual ChangeStatus ClearMessage() => this.PublishChange(this, null, () => this.AbstractionLayer.ClearHasMessage() ? ChangeStatus.Success : ChangeStatus.Failure);

		/// <summary>Gets the article path.</summary>
		/// <param name="unparsedPath">The unparsed path. This can be a local article path or an interwiki path.</param>
		/// <param name="articleName">Name of the article.</param>
		/// <param name="fragment">The fragment to jump to. May be null.</param>
		/// <returns>A full Uri to the article.</returns>
		/// <exception cref="ArgumentException">Article name is invalid.</exception>
		public virtual Uri GetArticlePath(string unparsedPath, string articleName, string fragment)
		{
			// Used to use WebUtility.UrlEncode, but Uri seems to auto-encode, so removed for now. Discussion in some places of different parts of .NET encoding differently, so may need to re-instate later. See https://stackoverflow.com/a/47877559/502255 for example.
			if (string.IsNullOrWhiteSpace(articleName))
			{
				throw new ArgumentException(CurrentCulture(TitleInvalid));
			}

			if (string.IsNullOrEmpty(unparsedPath))
			{
				unparsedPath = this.ArticlePath;
			}

			var fullPath = unparsedPath.Replace("$1", articleName.Replace(' ', '_')).TrimEnd('/');
			if (fragment != null)
			{
				fullPath += '#' + fragment;
			}

			return new Uri(fullPath);
		}

		/// <summary>Logs the user out.</summary>
		public virtual void Logout()
		{
			this.Clear();
			this.AbstractionLayer.Logout();
		}

		/// <summary>Raises the <see cref="Changing"/> event with the supplied arguments and indicates what actions should be taken.</summary>
		/// <param name="sender">The sending object.</param>
		/// <param name="parameters">A dictionary of parameters that were sent to the calling method.</param>
		/// <param name="changeFunction">The function to execute. It should return a <see cref="ChangeStatus"/> indicating whether the call was successful, failed, or ignored.</param>
		/// <param name="caller">The calling method (populated automatically with caller name).</param>
		/// <returns>A value indicating the actions that should take place.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Justification = "CallerMemberName requires it.")]
		public virtual ChangeStatus PublishChange(object sender, IReadOnlyDictionary<string, object> parameters, Func<ChangeStatus> changeFunction, [CallerMemberName] string caller = null)
		{
			ThrowNull(changeFunction, nameof(changeFunction));
			var changeArgs = new ChangeArgs(sender, caller, parameters);
			this.Changing?.Invoke(this, changeArgs);
			return
				changeArgs.CancelChange ? ChangeStatus.Cancelled :
				this.EditingEnabled ? changeFunction() :
				ChangeStatus.EditingDisabled;
		}

		/// <summary>Raises the Changing event with the supplied arguments and indicates what actions should be taken.</summary>
		/// <typeparam name="T">The return type for the ChangeValue object.</typeparam>
		/// <param name="sender">The sending object.</param>
		/// <param name="parameters">A dictionary of parameters that were sent to the calling method.</param>
		/// <param name="changeFunction">The function to execute. It should return a <see cref="ChangeValue{T}"/> object indicating whether the call was successful, failed, or ignored.</param>
		/// <param name="disabledResult">The value to use if the return status is <see cref="ChangeStatus.EditingDisabled"/>.</param>
		/// <param name="caller">The calling method (populated automatically with caller name).</param>
		/// <returns>A value indicating the actions that should take place.</returns>
		/// <remarks>In the event of a <see cref="ChangeStatus.Cancelled"/> result, the corresponding value will be <span class="keyword">default</span>.</remarks>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Justification = "CallerMemberName requires it.")]
		public virtual ChangeValue<T> PublishChange<T>(object sender, IReadOnlyDictionary<string, object> parameters, Func<ChangeValue<T>> changeFunction, T disabledResult, [CallerMemberName] string caller = null)
		{
			ThrowNull(changeFunction, nameof(changeFunction));
			var changeArgs = new ChangeArgs(sender, caller, parameters);
			this.Changing?.Invoke(this, changeArgs);
			return
				changeArgs.CancelChange ? new ChangeValue<T>(ChangeStatus.Cancelled, default) :
				this.EditingEnabled ? changeFunction() :
				new ChangeValue<T>(ChangeStatus.EditingDisabled, disabledResult);
		}

		/// <summary>Raises the <see cref="PageTextChanging"/> event with the supplied arguments and indicates what actions should be taken.</summary>
		/// <param name="changeArgs">The arguments involved in changing the page. The caller is responsible for creating the object so that it can get the various return values out of it when after the event.</param>
		/// <param name="changeFunction">The function to execute. It should return a <see cref="ChangeStatus"/> indicating whether the call was successful, failed, or ignored.</param>
		/// <returns>A value indicating the actions that should take place.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Justification = "CallerMemberName requires it.")]
		public virtual ChangeStatus PublishPageTextChange(PageTextChangeArgs changeArgs, Func<ChangeStatus> changeFunction)
		{
			ThrowNull(changeArgs, nameof(changeArgs));
			ThrowNull(changeFunction, nameof(changeFunction));
			this.PageTextChanging?.Invoke(this, changeArgs);
			var retval =
				changeArgs.CancelChange ? ChangeStatus.Cancelled :
				this.EditingEnabled ? changeFunction() :
				ChangeStatus.EditingDisabled;
			if (!this.EditingEnabled)
			{
				this.PagePreview?.Invoke(this, new PagePreviewArgs(changeArgs));
			}

			return retval;
		}

		/// <summary>Publishes a warning.</summary>
		/// <param name="sender">The sending object.</param>
		/// <param name="warning">The warning.</param>
		public virtual void PublishWarning(IMessageSource sender, string warning) => this.WarningOccurred?.Invoke(this, new WarningEventArgs(sender, warning));
		#endregion

		#region Internal Methods
		internal string GetPreferredImageMagicWord(string search)
		{
			var entry = ImageMagicWords[search];
			string retval = null;
			foreach (var wordName in entry)
			{
				var magicWord = this.MagicWords[wordName];
				foreach (var value in magicWord.Aliases)
				{
					if (!value.Contains("$1"))
					{
						retval = wordName;
						break;
					}
				}
			}

			return retval;
		}
		#endregion

		#region Protected Virtual Methods

		/// <summary>Downloads a file.</summary>
		/// <param name="input">The input parameters.</param>
		protected virtual void Download(DownloadInput input) => this.AbstractionLayer.Download(input);

		/// <summary>Gets active blocks as specified by the input parameters.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A read-only list of <see cref="Block"/> objects, as specified by the input parameters.</returns>
		protected virtual IReadOnlyList<Block> LoadBlocks(BlocksInput input)
		{
			ThrowNull(input, nameof(input));
			input.Properties = BlocksProperties.User | BlocksProperties.By | BlocksProperties.Timestamp | BlocksProperties.Expiry | BlocksProperties.Reason | BlocksProperties.Flags;
			var result = this.AbstractionLayer.Blocks(input);
			var retval = new List<Block>(result.Count);
			foreach (var item in result)
			{
				retval.Add(new Block(item.User, item.By, item.Reason, item.Timestamp ?? DateTime.MinValue, item.Expiry ?? DateTime.MaxValue, item.Flags, item.Automatic));
			}

			return retval;
		}

		/// <summary>Loads the disambiguation templates for wikis that don't use Disambiguator.</summary>
		/// <returns>A collection of titles of disambiguation templates.</returns>
		protected virtual ICollection<Title> LoadDisambiguationTemplates()
		{
			this.disambiguationTemplates = new HashSet<Title>();
			var page = new Page(this.Namespaces[MediaWikiNamespaces.MediaWiki], "Disambiguationspage");
			page.Load(PageModules.Default | PageModules.Links);
			if (page.Exists)
			{
				if (page.Links.Count == 0)
				{
					this.disambiguationTemplates.Add(new Title(this, page.Text.Trim()));
				}
				else
				{
					this.disambiguationTemplates.UnionWith(page.Links);
				}
			}

			return this.disambiguationTemplates;
		}

		/// <summary>Gets one or more messages from MediaWiki space.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A read-only dictionary of message names and their associated <see cref="MessagePage"/> objects, as specified by the input parameters.</returns>
		protected virtual IReadOnlyDictionary<string, MessagePage> LoadMessages(AllMessagesInput input)
		{
			var result = this.AbstractionLayer.AllMessages(input);
			var retval = new Dictionary<string, MessagePage>(result.Count);
			foreach (var item in result)
			{
				retval.Add(item.Name, new MessagePage(this, item));
			}

			return retval.AsReadOnly();
		}

		/// <summary>Gets recent changes as specified by the input parameters.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A read-only list of <see cref="RecentChange"/> objects, as specified by the input parameters.</returns>
		protected virtual IReadOnlyList<RecentChange> LoadRecentChanges(RecentChangesInput input)
		{
			var result = this.AbstractionLayer.RecentChanges(input);
			var retval = new List<RecentChange>(result.Count);
			foreach (var item in result)
			{
				retval.Add(new RecentChange(this, item));
			}

			return retval;
		}

		/// <summary>Gets user information as specified by the input parameters.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A read-only list of <see cref="User"/> objects, as specified by the input parameters.</returns>
		protected virtual IReadOnlyList<User> LoadUserInformation(UsersInput input)
		{
			var result = this.AbstractionLayer.Users(input);
			var retval = new List<User>(result.Count);
			foreach (var item in result)
			{
				retval.Add(new User(this, item));
			}

			return retval.AsReadOnly();
		}

		/// <summary>Gets a list of users on the wiki, as specified by the input parameters.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A read-only list of <see cref="User"/> objects, as specified by the input parameters.</returns>
		protected virtual IReadOnlyList<User> LoadUsers(AllUsersInput input)
		{
			ThrowNull(input, nameof(input));
			input.Properties = AllUsersProperties.None;
			var result = this.AbstractionLayer.AllUsers(input);
			var retval = new List<User>(result.Count);
			foreach (var item in result)
			{
				retval.Add(new User(this, item.Name));
			}

			return retval;
		}

		/// <summary>Logs the specified user into the wiki and loads necessary information for proper functioning of the class.</summary>
		/// <param name="input">The input parameters. May be null.</param>
		/// <exception cref="UnauthorizedAccessException">Thrown if there was an error logging into the wiki (which typically denotes that the user had the wrong password or does not have permission to log in).</exception>
		/// <remarks>Even if you wish to edit anonymously, you <em>must</em> still log in by passing <see langword="null" /> for the input.</remarks>
		protected virtual void Login(LoginInput input)
		{
			var result = this.AbstractionLayer.Login(input);
			if (result.Result != "Success")
			{
				this.Clear();
				throw new UnauthorizedAccessException(CurrentCulture(LoginFailed, result.Reason));
			}

			this.User = new User(this, result.User);
			this.UserFunctions = this.FindBestUserFunctions();
			this.UserFunctions.DoSiteCustomizations();

			// This should never happen with co-initialization, but just in case there's a massive change to the abstraction layer, make sure we have all the info we need.
			if (this.Version == null)
			{
				var siteInfo = this.AbstractionLayer.SiteInfo(new SiteInfoInput() { Properties = NeededSiteInfo });
				this.ParseInternalSiteInfo(siteInfo);
			}
		}

		/// <summary>Gets all site information required for proper functioning of the framework.</summary>
		/// <summary>Parses all site information that's needed internally for the Site object to work.</summary>
		/// <param name="siteInfo">The site information.</param>
		protected virtual void ParseInternalSiteInfo(SiteInfoResult siteInfo)
		{
			ThrowNull(siteInfo, nameof(siteInfo));

			// General
			this.CaseSensitive = siteInfo.Flags.HasFlag(SiteInfoFlags.CaseSensitive);
			this.Culture = GetCulture(siteInfo.Language);
			this.Name = siteInfo.SiteName;
			this.ServerName = siteInfo.ServerName;
			this.Version = siteInfo.Generator;
			var path = siteInfo.ArticlePath;
			if (path.StartsWith("/", StringComparison.Ordinal))
			{
				// If article path is relative, figure out the absolute address.
				var repl = path.Substring(0, path.IndexOf("$1", StringComparison.Ordinal));
				var articleBaseIndex = siteInfo.BasePage.IndexOf(repl, StringComparison.Ordinal);
				if (articleBaseIndex < 0)
				{
					articleBaseIndex = siteInfo.BasePage.IndexOf("/", siteInfo.BasePage.IndexOf("//", StringComparison.Ordinal) + 2, StringComparison.Ordinal);
				}

				path = siteInfo.BasePage.Substring(0, articleBaseIndex) + path;
			}

			this.ArticlePath = path;

			// NamespaceAliases
			var allAliases = new Dictionary<int, List<string>>();
			foreach (var item in siteInfo.NamespaceAliases)
			{
				if (!allAliases.TryGetValue(item.Id, out var list))
				{
					list = new List<string>();
					allAliases.Add(item.Id, list);
				}

				list.Add(item.Alias);
			}

			// Namespaces
			var namespaces = new List<Namespace>(siteInfo.Namespaces.Count);
			foreach (var item in siteInfo.Namespaces)
			{
				allAliases.TryGetValue(item.Id, out var aliases);
				namespaces.Add(new Namespace(this, item, aliases));
			}

			this.Namespaces = new NamespaceCollection(namespaces, this.EqualityComparerInsensitive);
			this.MainPage = new Page(this, siteInfo.MainPage); // Now that we understand namespaces, we can create a Page.

			// MagicWords
			foreach (var word in siteInfo.MagicWords)
			{
				this.magicWords.Add(word.Name, new MagicWord(word));
			}

			this.DisambiguatorAvailable = this.magicWords.ContainsKey("disambiguation");
			this.AddImageRegexes();

			// InterwikiMap
			var doGuess = true;
			foreach (var item in siteInfo.InterwikiMap)
			{
				if (item.Flags.HasFlag(InterwikiMapFlags.LocalInterwiki))
				{
					doGuess = false;
					break;
				}
			}

			var server = siteInfo.Server; // Used to help determine if interwiki is local
			var interwikiList = new List<InterwikiEntry>();
			foreach (var item in siteInfo.InterwikiMap)
			{
				var entry = new InterwikiEntry(this, item);
				if (doGuess)
				{
					entry.GuessLocalWikiFromServer(server);
				}

				interwikiList.Add(entry);
			}

			this.InterwikiMap = new InterwikiMap(interwikiList);
		}

		/// <summary>Patrols the specified Recent Changes ID.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns><c>true</c> if the edit was successfully patrolled; otherwise, <c>false</c>.</returns>
		protected virtual PatrolResult Patrol(PatrolInput input) => this.AbstractionLayer.Patrol(input);

		/// <summary>Uploads a file.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns><c>true</c> if the file was successfully uploaded.</returns>
		protected virtual bool Upload(UploadInput input) => this.AbstractionLayer.Upload(input).Result == "Success";
		#endregion

		#region Private Methods

		// Parse co-initialization results.
		private void AbstractionLayer_Initialized(IWikiAbstractionLayer sender, InitializationEventArgs eventArgs) => this.ParseInternalSiteInfo(eventArgs.Result);

		// Setup co-initialization to avoid near-duplicate requests with AbstractionLayer.
		private void AbstractionLayer_Initializing(IWikiAbstractionLayer sender, InitializationEventArgs eventArgs)
		{
			eventArgs.Properties = NeededSiteInfo;
			eventArgs.FilterLocalInterwiki = Filter.Any;
		}

		/// <summary>Forwards warning events from the abstraction layer to the wiki.</summary>
		/// <param name="sender">The sending abstraction layer.</param>
		/// <param name="eventArgs">The event arguments.</param>
		private void AbstractionLayer_WarningOccurred(IWikiAbstractionLayer sender, /* Overlapping type names, so must use full name here */ WallE.Design.WarningEventArgs eventArgs)
		{
			var warning = eventArgs.Warning;
			this.PublishWarning(this, "(" + warning.Code + ") " + warning.Info);
		}

		private void AddImageRegexes()
		{
			if (this.ImageParameterRegexes.Count > 0)
			{
				return;
			}

			foreach (var entry in ImageMagicWords)
			{
				var sb = new StringBuilder();
				sb.Append(@"\A");
				var needParentheses = entry.Value.Length != 1 || this.MagicWords[entry.Value[0]].Aliases.Count != 1;
				if (needParentheses)
				{
					sb.Append('(');
				}

				foreach (var wordName in entry.Value)
				{
					// Assumes that there is always at least one alias per word.
					var magicWord = this.MagicWords[wordName];
					if (!magicWord.CaseSensitive)
					{
						sb.Append("(?i-:");
					}

					foreach (var value in magicWord.Aliases)
					{
						var regexValue = value.Replace("$1", "(?<value>.*?)");
						sb
							.Append(regexValue)
							.Append('|');
					}

					if (!magicWord.CaseSensitive)
					{
						sb.Append(')');
					}

					sb
						.Remove(sb.Length - 1, 1)
						.Append("|");
				}

				sb.Remove(sb.Length - 1, 1);
				if (needParentheses)
				{
					sb.Append(')');
				}

				sb.Append(@"\Z");

				var regex = new Regex(sb.ToString(), RegexOptions.IgnoreCase);
				this.ImageParameterRegexes.Add(entry.Key, regex);
			}
		}

		private void Clear()
		{
			this.ArticlePath = null;
			this.magicWords.Clear();

			this.CaseSensitive = false;
			this.Culture = CultureInfo.CurrentCulture;
			this.DisambiguatorAvailable = false;
			this.MainPage = null;
			this.Name = null;
			this.Namespaces = null;
			this.ServerName = null;
			this.Version = null;
			if (!(this.UserFunctions is DefaultUserFunctions))
			{
				this.UserFunctions = DefaultUserFunctions.CreateInstance(this);
			}
		}

		private UserFunctions FindBestUserFunctions()
		{
			if (!UserFunctionsClasses.TryGetValue(string.Concat(this.ServerName, '/', this.User.Name), out var factory) &&
				!UserFunctionsClasses.TryGetValue(string.Concat(this.ServerName, '/'), out factory) &&
				!UserFunctionsClasses.TryGetValue(string.Concat('/', this.User.Name), out factory))
			{
				factory = DefaultUserFunctions.CreateInstance;
			}

			return factory(this);
		}
		#endregion
	}
}
