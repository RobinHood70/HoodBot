﻿namespace RobinHood70.Robby;

// TODO: Review access rights project-wide.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using RobinHood70.CommonCode;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.Robby.Properties;
using RobinHood70.WallE.Base;
using RobinHood70.WallE.Clients;
using RobinHood70.WallE.Design;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;
using RobinHood70.WikiCommon.Parser.Basic;

#region Public Delegates

/// <summary>Represents a method which creates a <see cref="Site"/> derivative.</summary>
/// <param name="abstractionLayer">The <see cref="IWikiAbstractionLayer"/> to initialize the site with.</param>
/// <returns>A custom <see cref="Site"/> object.</returns>
public delegate Site SiteFactoryMethod(IWikiAbstractionLayer abstractionLayer);
#endregion

#region Public Enumerations

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
#endregion

/// <summary>Represents a single wiki site.</summary>
/// <seealso cref="IMessageSource" />
[SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "Ironic suppression of buggy 'Remove unnecessary suppression'")]
[SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Sufficiently maintainable for now. Could conceivably split off the LoadX() methods if needed, I suppose.")]
public class Site : IMessageSource
{
	#region Private Constants
	private const SiteInfoProperties NeededSiteInfo =
		SiteInfoProperties.General |
		SiteInfoProperties.Namespaces |
		SiteInfoProperties.NamespaceAliases |
		SiteInfoProperties.MagicWords |
		SiteInfoProperties.InterwikiMap;
	#endregion

	#region Static Fields
	private static readonly string[] DefaultRedirect = ["#REDIRECT"];
	private static readonly Dictionary<string, SiteFactoryMethod> SiteClasses = new(StringComparer.Ordinal);
	#endregion

	#region Fields
	private readonly Dictionary<string, MagicWord> magicWords = new(StringComparer.Ordinal);
	private readonly PageNameComparer[] pageNameComparers = new PageNameComparer[2];

	private string? baseArticlePath;
	private CultureInfo culture = CultureInfo.CurrentCulture;
	private TitleCollection? deletePreventionTemplates;
	private TitleCollection? deletionCategories;
	private IReadOnlySet<Title>? disambiguationTemplates;
	private TitleCollection? discussionPages;
	private string? generator;
	private ReadOnlyKeyedCollection<string, InterwikiEntry>? interwikiMap;
	private FullTitle? mainPage;
	private string? mainPageName;
	private NamespaceCollection? namespaces;
	private string? scriptPath;
	private string? serverName;
	private string? siteName;
	#endregion

	#region Constructors

	/// <summary>Initializes a new instance of the <see cref="Site"/> class.</summary>
	/// <param name="wiki">The <see cref="IWikiAbstractionLayer"/> to use. This controls whether the API is used or some other access method.</param>
	public Site(IWikiAbstractionLayer wiki)
	{
		ArgumentNullException.ThrowIfNull(wiki);
		this.AbstractionLayer = wiki;
		wiki.Initializing += AbstractionLayer_Initializing;
		wiki.Initialized += this.AbstractionLayer_Initialized;
		wiki.WarningOccurred += this.AbstractionLayer_WarningOccurred;
		this.FilterPages = new TitleCollection(this);
		this.deletePreventionTemplates = new TitleCollection(this);
		this.deletionCategories = new TitleCollection(this);
		this.discussionPages = new TitleCollection(this);
	}
	#endregion

	#region Public Events

	/// <summary>Occurs when a change is about to be made to the wiki. Subscribers have the option to indicate if they need the change to be cancelled.</summary>
	public event StrongEventHandler<Site, ChangeArgs>? Changing;

	/// <summary>Occurs after the PageTextChanging event when a page is about to be edited on the wiki.</summary>
	public event StrongEventHandler<Site, PagePreviewArgs>? PagePreview;

	/// <summary>Occurs when a page is about to be edited on the wiki. Subscribers may make additional changes to the page, or indicate if they need the change to be cancelled.</summary>
	public event StrongEventHandler<Site, PageTextChangeArgs>? PageTextChanging;

	/// <summary>Occurs when a warning should be sent to the user.</summary>
	public event StrongEventHandler<Site, WarningEventArgs>? WarningOccurred;
	#endregion

	#region Public Properties

	/// <summary>Gets the wiki abstraction layer.</summary>
	/// <value>The wiki abstraction layer.</value>
	public IWikiAbstractionLayer AbstractionLayer { get; } // TODO: Hide this and replace it with public or internal methods for all related calls (e.g., delete page, get watchlist, etc.)

	/// <summary>Gets the base article path.</summary>
	/// <value>The base article path, where <c>$1</c> should be replaced with the URL-encoded article title. </value>
	/// <exception cref="InvalidOperationException">Thrown when the Site hasn't been initialized.</exception>
	public string BaseArticlePath => this.baseArticlePath ?? throw NoSite();

	/// <summary>Gets or sets a CultureInfo object base the wiki's language and variant.</summary>
	/// <value>The culture of the wiki.</value>
	/// <remarks>Not all languages available in MediaWiki have direct equivalents in Windows. The bot will attempt to fall back to the more general language or variant when possible, but this property is left settable in the event that the choice made is unacceptable. If the culture cannot be determined, <see cref="CultureInfo.CurrentCulture"/> is used instead. Attempting to set the Culture to null will also result in CurrentCulture being used.</remarks>
	/// <exception cref="InvalidOperationException">Thrown when the Site hasn't been initialized.</exception>
	public CultureInfo Culture
	{
		get => this.culture ?? throw NoSite();
		set
		{
			this.culture = value;
			this.pageNameComparers[0] = new PageNameComparer(value, false);
			this.pageNameComparers[1] = new PageNameComparer(value, true);
		}
	}

	/// <summary>Gets or sets the default load options.</summary>
	/// <value>The default load options.</value>
	/// <remarks>If you need to detect disambiguations, you should consider setting this to include Properties for wikis using Disambiguator or Templates for those that aren't.</remarks>
	public PageLoadOptions DefaultLoadOptions { get; set; } = PageLoadOptions.Default;

	/// <summary>Gets a list of templates indicating a page should never be flagged for deletion.</summary>
	/// <value>A list of templates indicating a page should never be flagged for deletion.</value>
	public TitleCollection DeletePreventionTemplates => this.deletePreventionTemplates ?? this.LoadDeletePreventionTemplates();

	/// <summary>Gets a list of templates indicating a page is flagged for deletion.</summary>
	/// <value>A list of templates indicating a page is flagged for deletion.</value>
	public TitleCollection DeletionCategories => this.deletionCategories ?? this.LoadDeletionCategories();

	/// <summary>Gets the list of disambiguation templates on wikis that aren't using Disambiguator.</summary>
	/// <value>The disambiguation templates.</value>
	/// <remarks>This will be auto-populated on first use if not already set.</remarks>
	public IReadOnlySet<Title> DisambiguationTemplates => this.disambiguationTemplates ??= this.LoadDisambiguationTemplates();

	/// <summary>Gets a list of pages that function as talk pages, but are located outside of traditional Talk spaces.</summary>
	public TitleCollection DiscussionPages => this.discussionPages ??= this.LoadDiscussionPages();

	/// <summary>Gets a value indicating whether the Disambiguator extension is available.</summary>
	/// <value><see langword="true"/> if the Disambiguator extension is available; otherwise, <see langword="false"/>.</value>
	public bool DisambiguatorAvailable { get; private set; }

	/// <summary>Gets or sets a value indicating whether methods that would alter the wiki should be disabled.</summary>
	/// <value><see langword="true"/> if editing should be disabled; otherwise, <see langword="false"/>.</value>
	/// <remarks>If set to true, most methods will silently fail, and their return <see cref="ChangeStatus.EditingDisabled"/>. This is primarily intended for testing new bot jobs without risking any unintended edits.</remarks>
	public bool EditingEnabled { get; set; }

	/// <summary>Gets the list of special pages on the site that should normally be filtered out of any results.</summary>
	/// <value>The filter pages.</value>
	public TitleCollection FilterPages { get; }

	/// <summary>Gets the MediaWiki version of the wiki, including any text.</summary>
	/// <value>The MediaWiki version of the wiki.</value>
	/// <exception cref="InvalidOperationException">Thrown when the Site hasn't been initialized.</exception>
	public string Generator => this.generator ?? throw NoSite();

	/// <summary>Gets the interwiki map.</summary>
	/// <value>The interwiki map.</value>
	/// <exception cref="InvalidOperationException">Thrown when the Site hasn't been initialized.</exception>
	public ReadOnlyKeyedCollection<string, InterwikiEntry> InterwikiMap => this.interwikiMap ?? throw NoSite();

	/// <summary>Gets a list of current magic words on the wiki.</summary>
	/// <value>The magic words.</value>
	public IReadOnlyDictionary<string, MagicWord> MagicWords => this.magicWords;

	/// <summary>Gets the <see cref="Title"/> for the main page of the site.</summary>
	/// <value>The main page.</value>
	/// <exception cref="InvalidOperationException">Thrown when the Site hasn't been initialized.</exception>
	public FullTitle MainPage => this.mainPage ?? throw NoSite();

	/// <summary>Gets the name of the main page, as returned by the site.</summary>
	/// <value>The name of the main page.</value>
	/// <remarks>This will normally be the same as <c><see cref="MainPage"/>.FullPageName()</c>, but is provided so that the original name is available, if needed.</remarks>
	/// <exception cref="InvalidOperationException">Thrown when the Site hasn't been initialized.</exception>
	public string MainPageName => this.mainPageName ?? throw NoSite();

	/// <summary>Gets the wiki name.</summary>
	/// <value>The name of the wiki.</value>
	/// <exception cref="InvalidOperationException">Thrown when the Site hasn't been initialized.</exception>
	public string Name => this.siteName ?? throw NoSite();

	/// <summary>Gets the wiki namespaces.</summary>
	/// <value>the wiki namespaces.</value>
	/// <exception cref="InvalidOperationException">Thrown when the Site hasn't been initialized.</exception>
	public NamespaceCollection Namespaces => this.namespaces ?? throw NoSite();

	/// <summary>Gets or sets the page creator.</summary>
	/// <value>The page creator.</value>
	/// <remarks>A PageCreator is an abstract factory which serves as a bridge between customized PageItem types from WallE and the corresponding custom Page type for Robby.</remarks>
	public PageCreator PageCreator { get; set; } = new DefaultPageCreator();

	/// <summary>Gets the script path. This is the path preceding api.php, index.php and so forth.</summary>
	/// <value>The script path.</value>
	/// <remarks>If not returned by the API, it will be guessed based on the path to api.php itself.</remarks>
	/// <exception cref="InvalidOperationException">Thrown when the Site hasn't been initialized.</exception>
	public string ScriptPath => this.scriptPath ?? throw NoSite();

	/// <summary>Gets the name of the server—typically, the base URL.</summary>
	/// <value>The name of the server.</value>
	/// <exception cref="InvalidOperationException">Thrown when the Site hasn't been initialized.</exception>
	public string ServerName => this.serverName ?? throw NoSite();

	/// <summary>Gets the bot's user name.</summary>
	/// <value>The bot's user name.</value>
	public User? User { get; private set; }

	/// <summary>Gets the MediaWiki version of the wiki.</summary>
	/// <value>The MediaWiki version of the wiki.</value>
	/// <remarks>If null, the wiki has not yet been initialized.</remarks>
	public Version? Version { get; private set; }
	#endregion

	#region Public Indexers

	/// <summary>Gets the <see cref="Namespace"/> with the specified key.</summary>
	/// <param name="id">The ID of the namespace.</param>
	/// <returns>The element with the specified key. If an element with the specified key is not found, an exception is thrown.</returns>
	/// <exception cref="KeyNotFoundException">An element with the specified key does not exist in the collection.</exception>
	/// <remarks>This is simply short-hand for <c>this.Namespaces[id]</c>.</remarks>
	public Namespace this[int id] => this.Namespaces[id];

	/// <summary>Gets the <see cref="Namespace"/> with the specified key.</summary>
	/// <param name="id">The name of the namespace.</param>
	/// <returns>The element with the specified key. If an element with the specified key is not found, an exception is thrown.</returns>
	/// <exception cref="KeyNotFoundException">An element with the specified key does not exist in the collection.</exception>
	/// <remarks>This is simply short-hand for <c>this.Namespaces[id]</c>.</remarks>
	public Namespace this[string id] => this.Namespaces[id];
	#endregion

	#region Public Static Methods

	/// <summary>The <see cref="SiteFactoryMethod"/> that creates a default <see cref="Site"/> object.</summary>
	/// <param name="abstractionLayer">The abstraction layer to use with the site.</param>
	/// <returns>A <see cref="Site"/> object.</returns>
	public static Site DefaultSiteFactoryMethod(IWikiAbstractionLayer abstractionLayer) => new(abstractionLayer);

	/// <summary>Gets a factory method to create a new <see cref="Site"/> object or derivative thereof.</summary>
	/// <param name="siteClassIdentifier">The site class identifier.</param>
	/// <returns>RobinHood70.Robby.SiteFactoryMethod.</returns>
	/// <remarks>Note that the identifier does <i>not</i> have to be the class name. It can be a user-friendly name, as long as the factory method for the <see cref="Site"/> derivative has been registered with <see cref="RegisterSiteClass(SiteFactoryMethod, string[])"/>. If the requested name is not found, the default <see cref="Site"/> factory method will be returned.</remarks>
	public static SiteFactoryMethod GetFactoryMethod(string? siteClassIdentifier) =>
		!string.IsNullOrEmpty(siteClassIdentifier) && SiteClasses.TryGetValue(siteClassIdentifier, out var factoryMethod)
			? factoryMethod
			: DefaultSiteFactoryMethod;

	/// <summary>Registers a user functions class under all site and username combinations.</summary>
	/// <param name="factoryMethod">The factory method that creates the correct UserFunctions class for the combination of site(s) and user(s).</param>
	/// <param name="identifiers">The identifier(s) associated with the class. This can be any value at all except null or <see cref="string.Empty"/> (both of which return the default <see cref="Site"/> object if used). This is done to allow user-friendly naming instead of lengthy type names (or shorter but potentially conflicting ones).</param>
	public static void RegisterSiteClass(SiteFactoryMethod factoryMethod, params string[] identifiers)
	{
		ArgumentNullException.ThrowIfNull(identifiers);
		foreach (var id in identifiers)
		{
			SiteClasses[id] = factoryMethod;
		}
	}
	#endregion

	#region Public Methods

	/// <summary>Creates a new, blank page.</summary>
	/// <param name="fullPageName">The full name of the page to create.</param>
	/// <returns>The newly created page. Note that this does not automatically save the page.</returns>
	public Page CreatePage(string fullPageName) => this.CreatePage(fullPageName, string.Empty);

	/// <summary>Creates a new, blank page.</summary>
	/// <param name="title">The title containing the name of the page to create.</param>
	/// <returns>The newly created page. Note that this does not automatically save the page.</returns>
	public Page CreatePage(Title title)
	{
		ArgumentNullException.ThrowIfNull(title);
		return this.CreatePage(title.FullPageName(), string.Empty);
	}

	/// <summary>Creates a new page with the specified text.</summary>
	/// <param name="fullPageName">The full name of the page to create.</param>
	/// <param name="text">The text of the page.</param>
	/// <returns>The newly created page. Note that this does not automatically save the page.</returns>
	public Page CreatePage(string fullPageName, string text) => this.CreatePage(TitleFactory.FromUnvalidated(this, fullPageName), text);

	/// <summary>Creates a new, blank page.</summary>
	/// <param name="ns">The namespace of the page to create.</param>
	/// <param name="pageName">The name of the page to create.</param>
	/// <param name="text">The text of the page.</param>
	/// <returns>The newly created page. Note that this does not automatically save the page.</returns>
	public Page CreatePage(int ns, string pageName, string text) => this.CreatePage(TitleFactory.FromUnvalidated(this[ns], pageName), text);

	/// <summary>Creates a new, blank page.</summary>
	/// <param name="title">The <see cref="Title"/> of the page to create.</param>
	/// <param name="text">The text of the page.</param>
	/// <returns>The newly created page. Note that this does not automatically save the page.</returns>
	public Page CreatePage(Title title, string text)
	{
		var retval = this.PageCreator.CreatePage(title);
		retval.Text = text;
		return retval;
	}

	/// <summary>Downloads a resource to a local file.</summary>
	/// <param name="resource">The location of the resource (typically, the a Uri path). This does <em>not</em> have to be located on the wiki.</param>
	/// <param name="fileName">Name of the file.</param>
	/// <remarks><paramref name="resource"/> is not a <see cref="Uri"/> in order to satisfy <see cref="IWikiAbstractionLayer"/>'s agnosticism. In practice, however, it will almost certainly always be one.</remarks>
	public bool Download(string resource, string? fileName) => this.Download(new DownloadInput(resource, fileName));

	/// <summary>Downloads the most recent version of a file from the wiki.</summary>
	/// <param name="pageName">Name of the page. You do not have to specify the File namespace, but you may if it's convenient.</param>
	/// <param name="fileName">Name of the file.</param>
	public void DownloadFile(string pageName, string fileName)
	{
		TitleCollection fileTitle = new(this, MediaWikiNamespaces.File, pageName);
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
	public Uri GetArticlePath(string articleName, string? fragment) => this.GetArticlePath(this.BaseArticlePath, articleName, fragment);

	/// <summary>Gets the page-name comparer with the specified sensitivity.</summary>
	/// <param name="caseSensitive">Whether the comparer should have first-letter case sensitivity.</param>
	/// <returns>The specified <see cref="PageNameComparer"/>.</returns>
	public PageNameComparer GetPageNameComparer(bool caseSensitive) => this.pageNameComparers[caseSensitive ? 1 : 0];

	/// <summary>Gets a <see cref="StringComparer"/> based on the site's culture.</summary>
	/// <param name="ignoreCase"><see langword="true"/> to specify that comparison operations be case-insensitive; <see langword="false"/> to specify that comparison operations be case-sensitive.</param>
	/// <returns>A <see cref="StringComparer"/> based on the site's culture.</returns>
	public StringComparer GetStringComparer(bool ignoreCase) => StringComparer.Create(this.Culture, ignoreCase);

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
	public IReadOnlyList<Block> LoadBlocks(DateTime? start, DateTime? end, Filter account, Filter ip, Filter range, Filter temporary) => this.LoadBlocks(new BlocksInput() { Start = start, End = end, FilterAccount = account, FilterIP = ip, FilterRange = range, FilterTemporary = temporary });

	/// <summary>Gets active blocks for the specified set of users.</summary>
	/// <param name="users">The users.</param>
	/// <returns>Active blocks for the specified set of users.</returns>
	public IReadOnlyList<Block> LoadBlocks(IEnumerable<string> users) => this.LoadBlocks(new BlocksInput(users));

	/// <summary>Gets active blocks for the specified IP address.</summary>
	/// <param name="ip">The IP address.</param>
	/// <returns>Active blocks for the specified IP address.</returns>
	public IReadOnlyList<Block> LoadBlocks(IPAddress ip) => this.LoadBlocks(new BlocksInput(ip));

	/// <summary>Gets a message from MediaWiki space.</summary>
	/// <param name="message">The message.</param>
	/// <param name="arguments">Optional arguments to substitute into the message.</param>
	/// <returns>The text of the message.</returns>
	public string? LoadMessage([Localizable(false)] string message, params string[] arguments) => this.LoadMessage(message, arguments as IEnumerable<string>);

	/// <summary>Gets a message from MediaWiki space.</summary>
	/// <param name="message">The message.</param>
	/// <param name="arguments">Optional arguments to substitute into the message.</param>
	/// <returns>The text of the message.</returns>
	public string? LoadMessage([Localizable(false)] string message, IEnumerable<string> arguments)
	{
		ArgumentNullException.ThrowIfNull(message);
		ArgumentNullException.ThrowIfNull(arguments);
		var messages = this.LoadMessages([message], arguments);
		messages.TryGetValue(message, out var retval);
		return retval is null
			? null
			: retval.IsMissing
				? null
				: retval.Text;
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

	/// <summary>This is a convenience method to quickly get the text of a single page.</summary>
	/// <param name="pageName">Name of the page.</param>
	/// <returns>The page.</returns>
	public Page? LoadPage(string pageName)
	{
		ArgumentNullException.ThrowIfNull(pageName);
		return this.LoadPage(TitleFactory.FromUnvalidated(this, pageName));
	}

	/// <summary>This is a convenience method to quickly get the text of a single page.</summary>
	/// <param name="title">Name of the page.</param>
	/// <returns>The page.</returns>
	public Page? LoadPage(Title title)
	{
		var pages = PageCollection.Unlimited(this);
		pages.GetTitles(title);
		return pages.Count == 1 ? pages[0] : null;
	}

	/// <summary>This is a convenience method to quickly get the text of a single page.</summary>
	/// <param name="title">Name of the page.</param>
	/// <param name="subPageName">The subpage to get.</param>
	/// <returns>The page.</returns>
	public Page? LoadPage(Title title, string subPageName)
	{
		ArgumentNullException.ThrowIfNull(title);
		var titleName = title.PageName;
		if (!string.IsNullOrEmpty(subPageName))
		{
			if (subPageName[0] != '/')
			{
				titleName += '/';
			}

			titleName += subPageName;
		}

		var newTitle = TitleFactory.FromUnvalidated(title.Namespace, titleName);
		return this.LoadPage(newTitle);
	}

	/// <summary>This is a convenience method to quickly get the text of a single page.</summary>
	/// <param name="pageName">Name of the page.</param>
	/// <returns>The text of the page.</returns>
	public string? LoadPageText(string pageName)
	{
		ArgumentNullException.ThrowIfNull(pageName);
		return this.LoadPageText(TitleFactory.FromUnvalidated(this, pageName));
	}

	/// <summary>This is a convenience method to quickly get the text of a single page.</summary>
	/// <param name="title">Name of the page.</param>
	/// <returns>The text of the page.</returns>
	public string? LoadPageText(Title title)
	{
		var pages = PageCollection.Unlimited(this);
		pages.GetTitles(title);
		return pages.Count == 1 ? pages[0].Text : null;
	}

	/// <summary>This is a convenience method to quickly get the text of a single page.</summary>
	/// <param name="title">Name of the page.</param>
	/// <param name="subPageName">The subpage to get.</param>
	/// <returns>The text of the page.</returns>
	public string? LoadPageText(Title title, string subPageName)
	{
		ArgumentNullException.ThrowIfNull(title);
		var titleName = title.PageName;
		if (!string.IsNullOrEmpty(subPageName))
		{
			if (subPageName[0] != '/')
			{
				titleName += '/';
			}

			titleName += subPageName;
		}

		var newTitle = TitleFactory.FromUnvalidated(title.Namespace, titleName);
		return this.LoadPageText(newTitle);
	}

	/// <summary>Gets a message from MediaWiki space with any magic words and the like parsed into text.</summary>
	/// <param name="msg">The message.</param>
	/// <param name="arguments">Optional arguments to substitute into the message.</param>
	/// <returns>The text of the message.</returns>
	public string? LoadParsedMessage(string msg, params string[] arguments) => this.LoadParsedMessage(msg, arguments, null);

	/// <summary>Gets a message from MediaWiki space with any magic words and the like parsed into text.</summary>
	/// <param name="msg">The message.</param>
	/// <param name="arguments">Optional arguments to substitute into the message.</param>
	/// <returns>The text of the message.</returns>
	public string? LoadParsedMessage(string msg, IEnumerable<string> arguments) => this.LoadParsedMessage(msg, arguments, null);

	/// <summary>Gets a message from MediaWiki space with any magic words and the like parsed into text.</summary>
	/// <param name="msg">The message.</param>
	/// <param name="arguments">Optional arguments to substitute into the message.</param>
	/// <param name="title">The title to use for parsing.</param>
	/// <returns>The text of the message.</returns>
	public string? LoadParsedMessage(string msg, IEnumerable<string> arguments, Title? title)
	{
		ArgumentNullException.ThrowIfNull(msg);
		ArgumentNullException.ThrowIfNull(arguments);
		var messages = this.LoadParsedMessages([msg], arguments, title);
		return messages.TryGetValue(msg, out var retval) ? retval.Text : null;
	}

	/// <summary>Gets multiple messages from MediaWiki space with any magic words and the like parsed into text.</summary>
	/// <param name="messages">The messages.</param>
	/// <param name="arguments">Optional arguments to substitute into the message.</param>
	/// <returns>A read-only dictionary of the specified arguments with their associated Message objects.</returns>
	public IReadOnlyDictionary<string, MessagePage> LoadParsedMessages(IEnumerable<string> messages, IEnumerable<string> arguments) => this.LoadParsedMessages(messages, arguments, null);

	/// <summary>Gets multiple messages from MediaWiki space with any magic words and the like parsed into text.</summary>
	/// <param name="messages">The messages.</param>
	/// <param name="arguments">Optional arguments to substitute into the message.</param>
	/// <param name="context">The title to use for parsing.</param>
	/// <returns>A read-only dictionary of the specified arguments with their associated Message objects.</returns>
	public IReadOnlyDictionary<string, MessagePage> LoadParsedMessages(IEnumerable<string> messages, IEnumerable<string> arguments, Title? context) => this.LoadMessages(new AllMessagesInput
	{
		Messages = messages,
		Arguments = arguments,
		EnableParser = true,
		EnableParserTitle = context?.FullPageName(),
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
	/// <param name="newer">if set to <see langword="true"/>, returns changes from the specified date and newer; otherwise, it returns changes from the specified date and older.</param>
	/// <returns>A read-only list of recent changes before or after the specified date.</returns>
	public IReadOnlyList<RecentChange> LoadRecentChanges(DateTime start, bool newer) => this.LoadRecentChanges(start, newer, 0);

	/// <summary>Gets the specified number of recent changes before or after the specified date.</summary>
	/// <param name="start">The start date.</param>
	/// <param name="newer">if set to <see langword="true"/>, returns changes from the specified date and newer; otherwise, it returns changes from the specified date and older.</param>
	/// <param name="count">The number of recent changes to get.</param>
	/// <returns>A read-only list of recent changes before or after the specified date.</returns>
	public IReadOnlyList<RecentChange> LoadRecentChanges(DateTime start, bool newer, int count) => this.LoadRecentChanges(new RecentChangesOptions() { Start = start, Newer = newer, Count = count });

	/// <summary>Gets recent changes from or excluding the specified user.</summary>
	/// <param name="user">The user.</param>
	/// <param name="exclude">if set to <see langword="true"/>, get all recent changes <em>except</em> those from the specified user.</param>
	/// <returns>A read-only list of recent changes from or excluding the specified user.</returns>
	public IReadOnlyList<RecentChange> LoadRecentChanges(string user, bool exclude) => this.LoadRecentChanges(new RecentChangesOptions() { User = user, ExcludeUser = exclude });

	/// <summary>Gets recent changes according to a customized set of filter options.</summary>
	/// <param name="options">The filter options to apply.</param>
	/// <returns>A read-only list of recent changes that conform to the options specified.</returns>
	public virtual IReadOnlyList<RecentChange> LoadRecentChanges(RecentChangesOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);
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
	/// <param name="onlyActiveUsers">if set to <see langword="true"/>, only active users will be returned.</param>
	/// <param name="onlyUsersWithEdits">if set to <see langword="true"/>, only users with edits will be returned.</param>
	/// <returns>A list of users based on the specified criteria.</returns>
	public IReadOnlyList<User> LoadUsers(bool onlyActiveUsers, bool onlyUsersWithEdits) => this.LoadUsers(new AllUsersInput { ActiveUsersOnly = onlyActiveUsers, WithEditsOnly = onlyUsersWithEdits });

	/// <summary>Gets users whose names start with the specified prefix.</summary>
	/// <param name="onlyActiveUsers">if set to <see langword="true"/>, only active users will be returned.</param>
	/// <param name="onlyUsersWithEdits">if set to <see langword="true"/>, only users with edits will be returned.</param>
	/// <param name="prefix">The prefix.</param>
	/// <returns>A list of users based on the specified criteria.</returns>
	public IReadOnlyList<User> LoadUsers(bool onlyActiveUsers, bool onlyUsersWithEdits, string prefix) => this.LoadUsers(new AllUsersInput { ActiveUsersOnly = onlyActiveUsers, WithEditsOnly = onlyUsersWithEdits, Prefix = prefix });

	/// <summary>Gets users whose names fall within the specified range.</summary>
	/// <param name="onlyActiveUsers">if set to <see langword="true"/>, only active users will be returned.</param>
	/// <param name="onlyUsersWithEdits">if set to <see langword="true"/>, only users with edits will be returned.</param>
	/// <param name="from">The name to start at (inclusive). The name specified does not have to exist.</param>
	/// <param name="to">The name to stop at (inclusive). The name specified does not have to exist.</param>
	/// <returns>A list of users based on the specified criteria.</returns>
	public IReadOnlyList<User> LoadUsers(bool onlyActiveUsers, bool onlyUsersWithEdits, string from, string to) => this.LoadUsers(new AllUsersInput { ActiveUsersOnly = onlyActiveUsers, WithEditsOnly = onlyUsersWithEdits, From = from, To = to });

	/// <summary>Gets the users that belong to the specified groups.</summary>
	/// <param name="onlyActiveUsers">if set to <see langword="true"/>, only active users will be retrieved.</param>
	/// <param name="onlyUsersWithEdits">if set to <see langword="true"/>, only users with edits will be retrieved.</param>
	/// <param name="groups">The groups.</param>
	/// <returns>A list of <see cref="User"/> objects for users in the specified groups.</returns>
	public IReadOnlyList<User> LoadUsersInGroups(bool onlyActiveUsers, bool onlyUsersWithEdits, params string[] groups) => this.LoadUsersInGroups(onlyActiveUsers, onlyUsersWithEdits, groups as IEnumerable<string>);

	/// <summary>Gets users that belong to the specified groups.</summary>
	/// <param name="onlyActiveUsers">if set to <see langword="true"/>, only active users will be retrieved.</param>
	/// <param name="onlyUsersWithEdits">if set to <see langword="true"/>, only users with edits will be retrieved.</param>
	/// <param name="groups">The groups.</param>
	/// <returns>A list of <see cref="User"/> objects for users in the specified groups.</returns>
	public IReadOnlyList<User> LoadUsersInGroups(bool onlyActiveUsers, bool onlyUsersWithEdits, IEnumerable<string> groups) => this.LoadUsers(new AllUsersInput { ActiveUsersOnly = onlyActiveUsers, WithEditsOnly = onlyUsersWithEdits, Groups = groups });

	/// <summary>This is a convenience method to quickly get the text of a single subpage in the user's space.</summary>
	/// <param name="subPageName">Name of the user subpage.</param>
	/// <returns>The text of the page.</returns>
	public Page? LoadUserSubPage(string subPageName)
	{
		ArgumentException.ThrowIfNullOrEmpty(subPageName);
		return this.User is not User user
			? throw new InvalidOperationException("Not logged in.")
			: this.LoadPage(user.Title, subPageName);
	}

	/// <summary>This is a convenience method to quickly get the text of a single subpage in the user's space.</summary>
	/// <param name="subPageName">Name of the user subpage.</param>
	/// <returns>The text of the page.</returns>
	public string? LoadUserSubPageText(string subPageName)
	{
		ArgumentException.ThrowIfNullOrEmpty(subPageName);
		return this.User is not User user
			? throw new InvalidOperationException("Not logged in.")
			: this.LoadPageText(user.Title, subPageName);
	}

	/// <summary>Gets users that have the specified rights.</summary>
	/// <param name="onlyActiveUsers">if set to <see langword="true"/>, only active users will be retrieved.</param>
	/// <param name="onlyUsersWithEdits">if set to <see langword="true"/>, only users with edits will be retrieved.</param>
	/// <param name="rights">The rights.</param>
	/// <returns>A list of <see cref="User"/> objects for users with the specified rights.</returns>
	public IReadOnlyList<User> LoadUsersWithRights(bool onlyActiveUsers, bool onlyUsersWithEdits, params string[] rights) => this.LoadUsersWithRights(onlyActiveUsers, onlyUsersWithEdits, rights as IEnumerable<string>);

	/// <summary>Gets users that have the specified rights.</summary>
	/// <param name="onlyActiveUsers">if set to <see langword="true"/>, only active users will be retrieved.</param>
	/// <param name="onlyUsersWithEdits">if set to <see langword="true"/>, only users with edits will be retrieved.</param>
	/// <param name="rights">The rights.</param>
	/// <returns>A list of <see cref="User"/> objects for users with the specified rights.</returns>
	public IReadOnlyList<User> LoadUsersWithRights(bool onlyActiveUsers, bool onlyUsersWithEdits, IEnumerable<string> rights) => this.LoadUsers(new AllUsersInput { ActiveUsersOnly = onlyActiveUsers, WithEditsOnly = onlyUsersWithEdits, Rights = rights });

	/// <summary>Logs the specified user into the wiki and loads necessary information for proper functioning of the class.</summary>
	/// <param name="userName">Name of the user. This can be null if you wish to edit anonymously.</param>
	/// <param name="password">The password.</param>
	/// <exception cref="UnauthorizedAccessException">Thrown if there was an error logging into the wiki (which typically denotes that the user had the wrong password or does not have permission to log in).</exception>
	/// <remarks>Even if you wish to edit anonymously, you <em>must</em> still log in by passing <see langword="null" /> for the <paramref name="userName" /> parameter.</remarks>
	public void Login(string? userName, string? password)
	{
		if (userName is null)
		{
			this.Login(null);
		}
		else
		{
			this.Login(new LoginInput(userName, password ?? string.Empty));
		}
	}

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
	public ChangeStatus Patrol(long rcid)
	{
		Dictionary<string, object?> parameters = new(StringComparer.Ordinal)
		{
			[nameof(rcid)] = rcid,
		};

		return this.PublishChange(this, parameters, ChangeFunc);

		ChangeStatus ChangeFunc()
		{
			var retval = this.Patrol(new PatrolInput(rcid));

			return retval.Title == null
				? ChangeStatus.Failure
				: ChangeStatus.Success;
		}
	}

	/// <summary>Patrols the specified revision ID.</summary>
	/// <param name="revid">The revision ID.</param>
	/// <returns>A value indicating the change status of the patrol.</returns>
	public ChangeStatus PatrolRevision(long revid)
	{
		Dictionary<string, object?> parameters = new(StringComparer.Ordinal)
		{
			[nameof(revid)] = revid,
		};

		return this.PublishChange(this, parameters, ChangeFunc);

		ChangeStatus ChangeFunc()
		{
			var retval = this.Patrol(new PatrolInput(revid));
			return retval.Title == null
				? ChangeStatus.Failure
				: ChangeStatus.Success;
		}
	}

	/// <summary>Undoes an edit.</summary>
	/// <param name="title">The page the revision is on.</param>
	/// <param name="revisionId">The revision ID to undo.</param>
	/// <param name="editSummary">The edit summary.</param>
	/// <returns>A value indicating the change status of posting the new talk page message.</returns>
	/// <exception cref="InvalidOperationException">Thrown when the user's talk page is invalid.</exception>
	public ChangeStatus Undo(Title title, long revisionId, string editSummary)
	{
		ArgumentNullException.ThrowIfNull(title);
		ArgumentNullException.ThrowIfNull(editSummary);

		Dictionary<string, object?> parameters = new(StringComparer.Ordinal)
		{
			[nameof(title)] = title.FullPageName(),
			[nameof(revisionId)] = revisionId,
			[nameof(editSummary)] = editSummary
		};

		return this.PublishChange(this, parameters, ChangeFunc);

		ChangeStatus ChangeFunc()
		{
			EditInput input = new(title.FullPageName(), revisionId)
			{
				Bot = true,
				Minor = Tristate.True,
				Recreate = false,
				Summary = editSummary
			};

			var retval = this.AbstractionLayer.Edit(input);

			return retval.Result.OrdinalICEquals("Success")
				? (!retval.Flags.HasAnyFlag(EditFlags.NoChange))
					? ChangeStatus.Success
					: ChangeStatus.NoEffect
				: ChangeStatus.Failure;
		}
	}

	/// <summary>Unwatches all pages in the specified namespace.</summary>
	/// <param name="ns">The namespace to unwatch.</param>
	/// <returns>A collection of pages that were unwatched.</returns>
	public ChangeValue<PageCollection> Unwatch(int ns) => this.Watch(ns, true);

	/// <summary>Upload a file to the wiki.</summary>
	/// <param name="fileName">The full path and filename of the file to upload.</param>
	/// <param name="editSummary">The edit summary for the upload.</param>
	/// <remarks>The destination filename will be the same as the local filename.</remarks>
	/// <exception cref="ArgumentException">Path contains an invalid character.</exception>
	/// <returns>A value indicating the change status of the upload.</returns>
	public ChangeStatus Upload(string fileName, string editSummary) => this.Upload(fileName, null, editSummary, null, false);

	/// <summary>Upload a file to the wiki.</summary>
	/// <param name="fileName">The full path and filename of the file to upload.</param>
	/// <param name="destinationName">The bare name (i.e., do not include "File:") of the file to upload to on the wiki. Set to null to use the filename from the <paramref name="fileName" /> parameter.</param>
	/// <param name="editSummary">The edit summary for the upload.</param>
	/// <remarks>The destination filename will be the same as the local filename.</remarks>
	/// <exception cref="ArgumentException">Path contains an invalid character.</exception>
	/// <returns>A value indicating the change status of the upload.</returns>
	public ChangeStatus Upload(string fileName, string destinationName, string editSummary) => this.Upload(fileName, destinationName, editSummary, null, false);

	/// <summary>Upload a file to the wiki.</summary>
	/// <param name="fileName">The full path and filename of the file to upload.</param>
	/// <param name="destinationName">The bare name (i.e., do not include "File:") of the file to upload to on the wiki. Set to null to use the filename from the <paramref name="fileName" /> parameter.</param>
	/// <param name="editSummary">The edit summary for the upload.</param>
	/// <param name="pageText">Full page text for the File page. This should include the license, categories, and anything else required. Set to null  if this is a new version of an existing file or to allow the wiki to generate the page text (normally just the <paramref name="editSummary" />).</param>
	/// <exception cref="ArgumentException">Path contains an invalid character.</exception>
	/// <returns>A value indicating the change status of the upload.</returns>
	public ChangeStatus Upload(string fileName, string? destinationName, string editSummary, string? pageText) => this.Upload(fileName, destinationName, editSummary, pageText, false);

	/// <summary>Upload a file to the wiki.</summary>
	/// <param name="fileName">The full path and filename of the file to upload.</param>
	/// <param name="destinationName">The bare name (i.e., do not include "File:") of the file to upload to on the wiki. Set to null to use the filename from the <paramref name="fileName" /> parameter.</param>
	/// <param name="editSummary">The edit summary for the upload.</param>
	/// <param name="pageText">Full page text for the File page. This should include the license, categories, and anything else required. Set to null  if this is a new version of an existing file or to allow the wiki to generate the page text (normally just the <paramref name="editSummary" />).</param>
	/// <param name="ignoreWarnings">Whether to ignore upload warnings.</param>
	/// <exception cref="ArgumentException">Path contains an invalid character.</exception>
	/// <returns>A value indicating the change status of the upload.</returns>
	public ChangeStatus Upload(string fileName, string? destinationName, string editSummary, string? pageText, bool ignoreWarnings)
	{
		ArgumentNullException.ThrowIfNull(fileName);
		ArgumentNullException.ThrowIfNull(editSummary);

		// Always access this, even if we don't need it, as a means of checking validity.
		var checkedName = Path.GetFileName(fileName);
		if (string.IsNullOrWhiteSpace(destinationName))
		{
			destinationName = checkedName;
		}

		Dictionary<string, object?> parameters = new(StringComparer.Ordinal)
		{
			[nameof(fileName)] = fileName,
			[nameof(destinationName)] = destinationName,
			[nameof(editSummary)] = editSummary,
			[nameof(pageText)] = pageText,
			[nameof(ignoreWarnings)] = ignoreWarnings,
		};

		return !this.EditingEnabled && !File.Exists(fileName)
			? ChangeStatus.Failure
			: this.PublishChange(this, parameters, ChangeFunc);

		ChangeStatus ChangeFunc()
		{
			using FileStream upload = new(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
			UploadInput uploadInput = new(destinationName!, upload)
			{
				Comment = editSummary,
				IgnoreWarnings = ignoreWarnings,
				Text = pageText,
			};

			return this.Upload(uploadInput)
				? ChangeStatus.Success
				: ChangeStatus.Failure;
		}
	}

	/// <summary>Waits for the job queue to reach zero.</summary>
	/// <remarks>Waits for 10 seconds between each request.</remarks>
	public void WaitForJobQueue() => this.WaitForJobQueue(TimeSpan.FromSeconds(10), 0);

	/// <summary>If editing is enabled, waits for the job queue to reach zero.</summary>
	/// <param name="timeBetweenChecks">The amount of time to wait between requests for the number of jobs remaining.</param>
	/// <param name="atOrBelow">Wait until the job queue is at or below this value.</param>
	public void WaitForJobQueue(TimeSpan timeBetweenChecks, int atOrBelow)
	{
		if (!this.EditingEnabled)
		{
			return;
		}

		long jobsRemaining;
		var client = (this.AbstractionLayer as IInternetEntryPoint)?.Client;
		do
		{
			var input = new SiteInfoInput(SiteInfoProperties.Statistics);
			var results = this.AbstractionLayer.SiteInfo(input);
			jobsRemaining = results.Statistics?.Jobs ?? 0;
			Debug.WriteLine(jobsRemaining);
			if (jobsRemaining > atOrBelow)
			{
				if (client == null)
				{
					Thread.Sleep(timeBetweenChecks);
				}
				else
				{
					Debug.WriteLine("Request Delay Start");
					client.RequestDelay(timeBetweenChecks, DelayReason.ClientThrottled, Globals.CurrentCulture(Resources.WaitingForJobQueue, jobsRemaining));
				}
			}
		}
		while (jobsRemaining > 0);
	}

	/// <summary>Watches all pages in the specified namespace.</summary>
	/// <param name="ns">The namespace to watch.</param>
	/// <returns>A collection of pages that were watched.</returns>
	public ChangeValue<PageCollection> Watch(int ns) => this.Watch(ns, false);

	/// <summary>Watches or unwatches all pages in the specified namespace.</summary>
	/// <param name="ns">The namespace to watch or (un)watch.</param>
	/// <param name="unwatch">If set to <see langword="true"/>, pages will be unwatched; otherwise, pages will be watched.</param>
	/// <returns>A collection of pages that were (un)watched.</returns>
	public ChangeValue<PageCollection> Watch(int ns, bool unwatch)
	{
		var disabledResult = PageCollection.UnlimitedDefault(this);
		Dictionary<string, object?> parameters = new(StringComparer.Ordinal)
		{
			[nameof(ns)] = ns,
		};

		return this.PublishChange(disabledResult, this, parameters, ChangeFunc);

		ChangeValue<PageCollection> ChangeFunc()
		{
			AllPagesInput generatorInput = new() { Namespace = ns };
			WatchInput input = new(generatorInput) { Unwatch = unwatch };
			var pages = this.Watch(input);
			var result = pages.Count == 0
				? ChangeStatus.NoEffect
				: ChangeStatus.Success;
			return new ChangeValue<PageCollection>(result, pages);
		}
	}
	#endregion

	#region Public Virtual Methods

	/// <summary>Clears the bot's "has message" flag.</summary>
	/// <param name="force">Clears the message, even if the site is in read-only mode.</param>
	/// <returns><see cref="ChangeStatus.Success"/> if the flag was successfully cleared; otherwise, <see cref="ChangeStatus.Failure"/>.</returns>
	public virtual ChangeStatus ClearMessage(bool force)
	{
		return force
			? ChangeFunc()
			: this.PublishChange(this, ImmutableDictionary<string, object?>.Empty, ChangeFunc);

		ChangeStatus ChangeFunc() => this.AbstractionLayer.ClearHasMessage()
			? ChangeStatus.Success
			: ChangeStatus.Failure;
	}

	/// <summary>Creates a new account on the server.</summary>
	/// <param name="name">The account name.</param>
	/// <param name="password">The account password.</param>
	/// <param name="email">The account email. May be null.</param>
	/// <returns><see cref="ChangeStatus.Success"/> if the account was successfully created; otherwise, <see cref="ChangeStatus.Failure"/>.</returns>
	public virtual ChangeStatus CreateAccount(string name, string password, string email)
	{
		Dictionary<string, object?> parameters = new(StringComparer.Ordinal)
		{
			[nameof(name)] = name,
			[nameof(password)] = password,
			[nameof(email)] = email,
		};

		return this.PublishChange(this, parameters, ChangeFunc);

		ChangeStatus ChangeFunc()
		{
			CreateAccountInput input = new(name, password) { Email = email };
			var retval = this.AbstractionLayer.CreateAccount(input);
			return retval.Result.OrdinalICEquals("Success")
				? ChangeStatus.Success
				: ChangeStatus.Failure;
		}
	}

	/// <summary>Gets the article path.</summary>
	/// <param name="unparsedPath">The unparsed path. This can be a local article path or an interwiki path.</param>
	/// <param name="articleName">Name of the article.</param>
	/// <param name="fragment">The fragment to jump to. May be null.</param>
	/// <returns>A full Uri to the article.</returns>
	/// <exception cref="ArgumentException">Article name is invalid.</exception>
	public virtual Uri GetArticlePath(string unparsedPath, string articleName, string? fragment)
	{
		// CONSIDER: Used to use WebUtility.UrlEncode, but Uri seems to auto-encode, so removed for now. Discussion in some places of different parts of .NET encoding differently, so may need to re-instate later. See https://stackoverflow.com/a/47877559/502255 for example.
		if (string.IsNullOrWhiteSpace(articleName))
		{
			throw new ArgumentException(paramName: nameof(articleName), message: Resources.TitleInvalid);
		}

		if (string.IsNullOrEmpty(unparsedPath))
		{
			unparsedPath = this.BaseArticlePath;
		}

		var fullPath = unparsedPath.Replace("$1", articleName.Replace(' ', '_'), StringComparison.Ordinal).TrimEnd('/');
		if (fragment != null)
		{
			fullPath += '#' + fragment;
		}

		return new Uri(fullPath);
	}

	/// <summary>Gets the redirect target from the page text.</summary>
	/// <param name="text">The text to parse.</param>
	/// <returns>A <see cref="IFullTitle"/> object with the parsed redirect.</returns>
	public virtual FullTitle? GetRedirectFromText(string text) => this.GetRedirectFromTextInternal(text) is ILinkNode linkNode
		? TitleFactory.FromBacklinkNode(this, linkNode).ToFullTitle()
		: null;

	/// <summary>Gets all page property names in use on the wiki.</summary>
	/// <returns>All page property names on the wiki.</returns>
	public virtual IReadOnlyList<string> LoadPagePropertyNames() => this.AbstractionLayer.PagePropertyNames(new PagePropertyNamesInput());

	/// <summary>Logs the user out.</summary>
	/// <param name="force">If set to <see langword="true"/>, the logout will be performed, even if editing is disabled.</param>
	/// <remarks>Like all write operations, the actual logout request will NOT be sent to the wiki unless editing is enabled.</remarks>
	public virtual void Logout(bool force)
	{
		this.Clear();

		if (force || this.EditingEnabled)
		{
			this.AbstractionLayer.Logout();
		}
	}

	/// <summary>Moves the title to the name specified.</summary>
	/// <param name="from">The title to move.</param>
	/// <param name="to">Where to move the title to.</param>
	/// <param name="reason">The reason for the move.</param>
	/// <param name="suppressRedirect">if set to <see langword="true"/>, suppress the redirect that would normally be created.</param>
	/// <returns>A value indicating the change status of the move along with the list of pages that were moved and where they were moved to.</returns>
	/// <remarks>The original title object will remain unaltered after the move; it will not be updated to reflect the destination.</remarks>
	public ChangeValue<IDictionary<string, string>> Move(Title from, Title to, string reason, bool suppressRedirect) => this.Move(from, to, reason, false, false, suppressRedirect);

	/// <summary>Moves the title to the name specified.</summary>
	/// <param name="from">The title to move.</param>
	/// <param name="to">Where to move the title to.</param>
	/// <param name="reason">The reason for the move.</param>
	/// <param name="moveTalk">if set to <see langword="true"/>, moves the talk page as well as the original page.</param>
	/// <param name="moveSubpages">if set to <see langword="true"/>, moves all sub-pages of the original page.</param>
	/// <param name="suppressRedirect">if set to <see langword="true"/>, suppress the redirect that would normally be created.</param>
	/// <returns>A value indicating the change status of the move along with the list of pages that were moved and where they were moved to.</returns>
	/// <remarks>The original title object will remain unaltered after the move; it will not be updated to reflect the destination.</remarks>
	public ChangeValue<IDictionary<string, string>> Move(Title from, Title to, string reason, bool moveTalk, bool moveSubpages, bool suppressRedirect)
	{
		ArgumentNullException.ThrowIfNull(from);
		ArgumentNullException.ThrowIfNull(to);
		ArgumentNullException.ThrowIfNull(reason);

		const string subPageName = "/SubPage";
		Dictionary<string, string> disabledResult = new(StringComparer.Ordinal);
		if (!this.EditingEnabled)
		{
			disabledResult.Add(from.FullPageName(), to.FullPageName());
			if (moveTalk && from.TalkPage() is Title fromTalk)
			{
				disabledResult.Add(fromTalk.FullPageName(), to.TalkPage()?.FullPageName() ?? string.Empty);
			}

			if (moveSubpages && from.Namespace.AllowsSubpages)
			{
				var toSubPage = TitleFactory.FromUnvalidated(this, to + subPageName);
				disabledResult.Add(from.FullPageName() + subPageName, toSubPage.Title.FullPageName());
			}
		}

		Dictionary<string, object?> parameters = new(StringComparer.Ordinal)
		{
			[nameof(to)] = to,
			[nameof(reason)] = reason,
			[nameof(moveTalk)] = moveTalk,
			[nameof(moveSubpages)] = moveSubpages,
			[nameof(suppressRedirect)] = suppressRedirect,
		};

		return this.PublishChange(disabledResult, this, parameters, ChangeFunc);

		ChangeValue<IDictionary<string, string>> ChangeFunc()
		{
			MoveInput input = new(from.FullPageName(), to.FullPageName())
			{
				IgnoreWarnings = true,
				MoveSubpages = moveSubpages,
				MoveTalk = moveTalk,
				NoRedirect = suppressRedirect,
				Reason = reason
			};

			var status = ChangeStatus.Success;
			Dictionary<string, string> dict = new(StringComparer.Ordinal);
			try
			{
				var retval = this.AbstractionLayer.Move(input);
				foreach (var item in retval)
				{
					if (item.Error != null)
					{
						this.PublishWarning(this, Globals.CurrentCulture(Resources.MovePageWarning, from.FullPageName(), to, item.Error.Info));
					}
					else if (item.From != null && item.To != null)
					{
						dict.Add(item.From, item.To);
					}
					else
					{
						throw new InvalidOperationException(); // item.From and/or item.To was null.
					}
				}
			}
			catch (WikiException e)
			{
				this.PublishWarning(this, e.Info ?? e.Message);
				status = ChangeStatus.Failure;
			}

			return new ChangeValue<IDictionary<string, string>>(status, dict);
		}
	}

	/// <summary>Raises the <see cref="Changing"/> event with the supplied arguments and indicates what actions should be taken.</summary>
	/// <param name="sender">The sending object.</param>
	/// <param name="parameters">A dictionary of parameters that were sent to the calling method.</param>
	/// <param name="changeFunction">The function to execute. It should return a <see cref="ChangeStatus"/> indicating whether the call was successful, failed, or ignored.</param>
	/// <param name="caller">The calling method (populated automatically with caller name).</param>
	/// <returns>A value indicating the actions that should take place.</returns>
	public virtual ChangeStatus PublishChange(object sender, IReadOnlyDictionary<string, object?> parameters, Func<ChangeStatus> changeFunction, [CallerMemberName] string caller = "")
	{
		ArgumentNullException.ThrowIfNull(changeFunction);
		ChangeArgs changeArgs = new(sender, caller, parameters);
		this.Changing?.Invoke(this, changeArgs);
		return
			changeArgs.CancelChange ? ChangeStatus.Cancelled :
			this.EditingEnabled ? changeFunction() :
			ChangeStatus.EditingDisabled;
	}

	/// <summary>Raises the Changing event with the supplied arguments and indicates what actions should be taken.</summary>
	/// <typeparam name="T">The return type for the ChangeValue object.</typeparam>
	/// <param name="disabledResult">The value to use if the return status is <see cref="ChangeStatus.EditingDisabled"/>.</param>
	/// <param name="sender">The sending object.</param>
	/// <param name="parameters">A dictionary of parameters that were sent to the calling method.</param>
	/// <param name="changeFunction">The function to execute. It should return a <see cref="ChangeValue{T}"/> object indicating whether the call was successful, failed, or ignored.</param>
	/// <param name="caller">The calling method (populated automatically with caller name).</param>
	/// <returns>A value indicating the actions that should take place.</returns>
	/// <remarks>In the event of a <see cref="ChangeStatus.Cancelled"/> result, the corresponding value will be <span class="keyword">default</span>.</remarks>
	public virtual ChangeValue<T> PublishChange<T>(T disabledResult, object sender, IReadOnlyDictionary<string, object?> parameters, Func<ChangeValue<T>> changeFunction, [CallerMemberName] string caller = "")
		where T : class
	{
		// Note: disabledResult comes first in this call instead of last to prevent ambiguous calls when T is a string (i.e., same type as caller parameter).
		ArgumentNullException.ThrowIfNull(changeFunction);
		ChangeArgs changeArgs = new(sender, caller, parameters);
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
	public virtual ChangeStatus PublishPageTextChange(PageTextChangeArgs changeArgs, Func<ChangeStatus> changeFunction)
	{
		ArgumentNullException.ThrowIfNull(changeArgs);
		ArgumentNullException.ThrowIfNull(changeFunction);
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
	internal ILinkNode? GetRedirectFromTextInternal(string text)
	{
		ArgumentNullException.ThrowIfNull(text);
		var redirectAliases = this.magicWords.TryGetValue("redirect", out var redirect)
			? redirect.Aliases
			: DefaultRedirect;
		HashSet<string> redirects = new(redirectAliases, StringComparer.Ordinal);
		var nodes = WikiNodeFactory.DefaultInstance.Parse(text);

		// Is the text of the format TextNode, LinkNode?
		if (nodes.Count > 1 &&
			nodes[0] is ITextNode textNode &&
			nodes[1] is ILinkNode linkNode)
		{
			var searchText = textNode.Text.TrimEnd();

			// In most cases, searchText will now be the full redirect magic word (e.g., "#REDIRECT"), but old versions of MediaWiki used colons, so strip off a single colon (only one is allowed) if needed and re-trim.
			if (searchText.Length > 0 && searchText[^1] == ':')
			{
				searchText = searchText[0..^1].TrimEnd();
			}

			if (redirects.Contains(searchText))
			{
				return linkNode;
			}
		}

		return null;
	}
	#endregion

	#region Protected Static Methods

	/// <summary>Throws an exception indicating that the site has not been initialized.</summary>
	/// <param name="name">The name.</param>
	/// <returns>System.InvalidOperationException.</returns>
	protected static InvalidOperationException NoSite([CallerMemberName] string name = "") => new(Globals.CurrentCulture(Resources.SiteNotInitialized, name));
	#endregion

	#region Protected Virtual Methods

	/// <summary>Downloads a file.</summary>
	/// <param name="input">The input parameters.</param>
	protected virtual bool Download(DownloadInput input) => this.AbstractionLayer.Download(input);

	/// <summary>Gets active blocks as specified by the input parameters.</summary>
	/// <param name="input">The input parameters.</param>
	/// <returns>A read-only list of <see cref="Block"/> objects, as specified by the input parameters.</returns>
	protected virtual IReadOnlyList<Block> LoadBlocks(BlocksInput input)
	{
		ArgumentNullException.ThrowIfNull(input);
		input.Properties = BlocksProperties.User | BlocksProperties.By | BlocksProperties.Timestamp | BlocksProperties.Expiry | BlocksProperties.Reason | BlocksProperties.Flags;
		var result = this.AbstractionLayer.Blocks(input);
		List<Block> retval = new(result.Count);
		foreach (var item in result)
		{
			var user = item.User == null ? null : new User(this, item.User);
			var by = item.By == null ? null : new User(this, item.By);
			retval.Add(new Block(user, by, item.Reason, item.Timestamp ?? DateTime.MinValue, item.Expiry ?? DateTime.MaxValue, item.Flags, item.Automatic));
		}

		return retval;
	}

	/// <summary>When overridden in a derived class, loads the list of templates indicating a page should never be flagged for deletion.</summary>
	/// <returns>A list of templates indicating a page should never be flagged for deletion.</returns>
	/// <remarks>If not overridden, this will return an empty collection.</remarks>
	protected virtual TitleCollection LoadDeletePreventionTemplates() => this.deletePreventionTemplates ??= new TitleCollection(this);

	/// <summary>When overridden in a derived class, loads the list of templates indicating a page is flagged for deletion.</summary>
	/// <returns>A list of templates indicating a page is flagged for deletion.</returns>
	/// <remarks>If not overridden, this will return an empty collection.</remarks>
	protected virtual TitleCollection LoadDeletionCategories() => this.deletionCategories ??= new TitleCollection(this);

	/// <summary>Loads the disambiguation templates for wikis that don't use Disambiguator.</summary>
	/// <returns>A collection of titles of disambiguation templates.</returns>
	protected virtual IReadOnlySet<Title> LoadDisambiguationTemplates()
	{
		var retval = new HashSet<Title>();
		Title title = TitleFactory.FromValidated(this[MediaWikiNamespaces.MediaWiki], "Disambiguationspage");
		var page = title.Load(PageModules.Default | PageModules.Links, false);
		if (page.Exists)
		{
			if (page.Links.Count > 0)
			{
				retval.UnionWith(page.Links);
			}
			else
			{
				var parser = new SiteParser(page);
				foreach (var link in parser.LinkNodes)
				{
					retval.Add(SiteLink.FromLinkNode(this, link).Title);
				}

				if (retval.Count == 0)
				{
					var split = page.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
					foreach (var link in split)
					{
						retval.Add(TitleFactory.FromUnvalidated(this, link));
					}
				}
			}
		}

		return retval;
	}

	/// <summary>When overridden in a derived class, loads the list of pages that function as talk pages, but are located outside of traditional Talk spaces.</summary>
	/// <returns>A list of pages that function as talk pages.</returns>
	/// <remarks>If not overridden, this will return an empty collection.</remarks>
	protected virtual TitleCollection LoadDiscussionPages() => this.discussionPages = new TitleCollection(this);

	/// <summary>Gets one or more messages from MediaWiki space.</summary>
	/// <param name="input">The input parameters.</param>
	/// <returns>A read-only dictionary of message names and their associated <see cref="MessagePage"/> objects, as specified by the input parameters.</returns>
	protected virtual IReadOnlyDictionary<string, MessagePage> LoadMessages(AllMessagesInput input)
	{
		var result = this.AbstractionLayer.AllMessages(input);
		Dictionary<string, MessagePage> retval = new(result.Count, StringComparer.Ordinal);
		foreach (var item in result)
		{
			var factory = TitleFactory.FromValidated(this[MediaWikiNamespaces.MediaWiki], item.Name);
			retval.Add(item.Name, new MessagePage(factory, item));
		}

		return retval.AsReadOnly();
	}

	/// <summary>Gets recent changes as specified by the input parameters.</summary>
	/// <param name="input">The input parameters.</param>
	/// <returns>A read-only list of <see cref="RecentChange"/> objects, as specified by the input parameters.</returns>
	protected virtual IReadOnlyList<RecentChange> LoadRecentChanges(RecentChangesInput input)
	{
		var result = this.AbstractionLayer.RecentChanges(input);
		List<RecentChange> retval = new(result.Count);
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
		List<User> retval = new(result.Count);
		foreach (var item in result)
		{
			retval.Add(new User(TitleFactory.FromValidated(this[MediaWikiNamespaces.User], item.Name), item));
		}

		return retval.AsReadOnly();
	}

	/// <summary>Gets a list of users on the wiki, as specified by the input parameters.</summary>
	/// <param name="input">The input parameters.</param>
	/// <returns>A read-only list of <see cref="User"/> objects, as specified by the input parameters.</returns>
	protected virtual IReadOnlyList<User> LoadUsers(AllUsersInput input)
	{
		ArgumentNullException.ThrowIfNull(input);
		input.Properties = AllUsersProperties.None;
		var result = this.AbstractionLayer.AllUsers(input);
		List<User> retval = new(result.Count);
		foreach (var item in result)
		{
			retval.Add(new User(this, item));
		}

		return retval;
	}

	/// <summary>Logs the specified user into the wiki and loads necessary information for proper functioning of the class.</summary>
	/// <param name="input">The input parameters.</param>
	/// <exception cref="UnauthorizedAccessException">Thrown if there was an error logging into the wiki (which typically denotes that the user had the wrong password or does not have permission to log in).</exception>
	/// <remarks>Even if you wish to edit anonymously, you <em>must</em> still log in by passing <see langword="null" /> for the input.</remarks>
	protected virtual void Login(LoginInput? input)
	{
		this.Clear();
		if (input is null)
		{
			this.AbstractionLayer.Initialize();
			this.User = null;
		}
		else
		{
			var result = this.AbstractionLayer.Login(input);
			if (!result.Result.OrdinalICEquals("Success"))
			{
				throw new UnauthorizedAccessException(Globals.CurrentCulture(Resources.LoginFailed, result.Reason ?? string.Empty));
			}

			var name = result.User;
			this.User = name == null ? null : new User(this, name);
		}
	}

	/// <summary>Gets all site information required for proper functioning of the framework.</summary>
	/// <summary>Parses all site information that's needed internally for the Site object to work.</summary>
	/// <exception cref="InvalidOperationException">Thrown when the site information is not valid.</exception>
	protected virtual void ParseInternalSiteInfo()
	{
		var siteInfo = this.AbstractionLayer.AllSiteInfo;
		if (siteInfo == null
			|| siteInfo.General == null
			|| siteInfo.Namespaces.Count == 0
			|| siteInfo.MagicWords.Count == 0)
		{
			throw new InvalidOperationException(Resources.MissingSiteInfo);
		}

		// General
		var general = siteInfo.General;
		var server = general.Server; // Used to help determine if interwiki is local
		this.Culture = Globals.GetCulture(general.Language);
		this.siteName = general.SiteName;
		this.serverName = general.ServerName;
		this.generator = general.Generator;
		var versionFudged = Regex.Replace(general.Generator, @"[^0-9\.]", ".", RegexOptions.None, Globals.DefaultRegexTimeout);
		versionFudged = Regex.Replace(versionFudged, @"\.{2,}", ".", RegexOptions.None, Globals.DefaultRegexTimeout);
		versionFudged = versionFudged.Trim(TextArrays.Period);
		this.Version = new Version(versionFudged);
		var path = general.ArticlePath;
		var basePath = general.BasePage[..(general.BasePage.IndexOf(server, StringComparison.Ordinal) + server.Length)]; // Search for server in BasePage and extract everything from the start of BasePage to then. This effectively converts Server to canonical if it was protocol-relative.
		this.baseArticlePath = path.StartsWith('/') ? basePath + path : path;
		this.mainPageName = general.MainPage;
		this.scriptPath = basePath + general.Script;
		this.namespaces = new NamespaceCollection(this, siteInfo.Namespaces, siteInfo.NamespaceAliases);
		if (this.mainPageName != null)
		{
			this.mainPage = TitleFactory.FromUnvalidated(this, this.mainPageName); // Now that we understand namespaces, we can create a Title.
		}

		// MagicWords
		foreach (var word in siteInfo.MagicWords)
		{
			this.magicWords.Add(word.Name, word);
		}

		this.DisambiguatorAvailable = this.magicWords.ContainsKey("disambiguation");

		// InterwikiMap
		var doGuess = true;
		foreach (var item in siteInfo.InterwikiMap)
		{
			if (item.Flags.HasAnyFlag(InterwikiMapFlags.LocalInterwiki))
			{
				doGuess = false;
				break;
			}
		}

		List<InterwikiEntry> interwikiList = [];
		foreach (var item in siteInfo.InterwikiMap)
		{
			InterwikiEntry entry = new(this, item);
			if (doGuess && server != null)
			{
				entry.GuessLocalWikiFromServer(server);
			}

			interwikiList.Add(entry);
		}

		this.interwikiMap = new ReadOnlyKeyedCollection<string, InterwikiEntry>(
			item =>
			{
				ArgumentNullException.ThrowIfNull(item);
				return item.Prefix;
			},
			interwikiList,
			StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>Patrols the specified Recent Changes ID.</summary>
	/// <param name="input">The input parameters.</param>
	/// <returns><see langword="true"/> if the edit was successfully patrolled; otherwise, <see langword="false"/>.</returns>
	protected virtual PatrolResult Patrol(PatrolInput input) => this.AbstractionLayer.Patrol(input);

	/// <summary>Watches or unwatches pages as specified by the input parameters.</summary>
	/// <param name="input">The input parameters.</param>
	/// <returns>A collection of the pages that were watched or unwatched.</returns>
	protected virtual PageCollection Watch(WatchInput input)
	{
		var result = this.AbstractionLayer.Watch(input);
		return new PageCollection(this, result);
	}

	/// <summary>Uploads a file.</summary>
	/// <param name="input">The input parameters.</param>
	/// <returns><see langword="true"/> if the file was successfully uploaded.</returns>
	protected virtual bool Upload(UploadInput input)
	{
		var result = this.AbstractionLayer.Upload(input);
		foreach (var warning in result.Warnings)
		{
			this.PublishWarning(this, $"{warning.Key}: {warning.Value}");
		}

		foreach (var duplicate in result.Duplicates)
		{
			this.PublishWarning(this, "Duplicate: " + duplicate);
		}

		return result.Result.OrdinalICEquals("Success");
	}
	#endregion

	#region Private Static Methods

	// Setup co-initialization to avoid near-duplicate requests with AbstractionLayer.
	private static void AbstractionLayer_Initializing(IWikiAbstractionLayer sender, InitializingEventArgs eventArgs)
	{
		eventArgs.Properties = NeededSiteInfo;
		eventArgs.FilterLocalInterwiki = Filter.Any;
	}
	#endregion

	#region Private Methods

	// Parse co-initialization results.
	private void AbstractionLayer_Initialized(IWikiAbstractionLayer sender, EventArgs eventArgs) => this.ParseInternalSiteInfo();

	/// <summary>Forwards warning events from the abstraction layer to the wiki.</summary>
	/// <param name="sender">The sending abstraction layer.</param>
	/// <param name="eventArgs">The event arguments.</param>
	private void AbstractionLayer_WarningOccurred(IWikiAbstractionLayer sender, /* Overlapping type names, so must use full name here */ WallE.Design.WarningEventArgs eventArgs)
	{
		var warning = eventArgs.Warning;
		this.PublishWarning(this, "(" + warning.Code + ") " + warning.Info);
	}

	private void Clear()
	{
		this.baseArticlePath = null;
		this.Culture = CultureInfo.CurrentCulture;
		this.DisambiguatorAvailable = false;
		this.magicWords.Clear();
		this.mainPage = null;
		this.namespaces = null;
		this.serverName = null;
		this.siteName = null;
		this.generator = null;
		this.Version = null;
	}
	#endregion
}