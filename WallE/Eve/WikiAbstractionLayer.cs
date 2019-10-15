namespace RobinHood70.WallE.Eve
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.Globalization;
	using System.Net;
	using System.Text.RegularExpressions;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Clients;
	using RobinHood70.WallE.Design;
	using RobinHood70.WallE.Eve.Modules;
	using RobinHood70.WallE.Properties;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>An API-based implementation of the <see cref="IWikiAbstractionLayer" /> interface.</summary>
	/// <seealso cref="IWikiAbstractionLayer" />
	/// <seealso cref="IMaxLaggable" />
	[SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Not much to be done while maintaining the ease of abstraction (e.g., using index.php or a database layer). If anyone has a better design, I'm all ears!")]
	public class WikiAbstractionLayer : IWikiAbstractionLayer, IMaxLaggable
	{
		#region Internal Constants
		internal const int LimitSmall1 = 50;
		internal const int LimitSmall2 = 500;
		internal const string ApiDisabledCode = "apidisabled";
		private const SiteInfoProperties NeededSiteInfo =
			SiteInfoProperties.General |
			SiteInfoProperties.DbReplLag |
			SiteInfoProperties.Namespaces |
			SiteInfoProperties.InterwikiMap;
		#endregion

		#region Fields
		private readonly HashSet<string> interwikiPrefixes = new HashSet<string>(StringComparer.Create(CultureInfo.InvariantCulture, true));
		private readonly Dictionary<int, NamespacesItem> namespaces = new Dictionary<int, NamespacesItem>();
		private readonly List<ErrorItem> warnings = new List<ErrorItem>();
		private readonly WikiException notInitialized = new WikiException(string.Format(Messages.SiteNotInitialized, nameof(Login), nameof(Initialize)));
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="WikiAbstractionLayer" /> class.</summary>
		/// <param name="client">The client.</param>
		/// <param name="uri">The URI.</param>
		[SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "As above.")]
		public WikiAbstractionLayer(IMediaWikiClient client, Uri uri)
		{
			ThrowNull(client, nameof(client));
			ThrowNull(uri, nameof(uri));
			if (!uri.AbsolutePath.EndsWith("api.php", StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException(CurrentCulture(EveMessages.InvalidApi));
			}

			this.Client = client;
			this.Uri = uri;
			this.ModuleFactory = new ModuleFactory(this)
				.RegisterGenerator<CategoriesInput>(PropCategories.CreateInstance)
				.RegisterGenerator<DeletedRevisionsInput>(PropDeletedRevisions.CreateInstance)
				.RegisterGenerator<DuplicateFilesInput>(PropDuplicateFiles.CreateInstance)
				.RegisterGenerator<FileUsageInput>(PropFileUsage.CreateInstance)
				.RegisterGenerator<ImagesInput>(PropImages.CreateInstance)
				.RegisterGenerator<LinksInput>(PropLinks.CreateInstance)
				.RegisterGenerator<LinksHereInput>(PropLinksHere.CreateInstance)
				.RegisterGenerator<RedirectsInput>(PropRedirects.CreateInstance)
				.RegisterGenerator<RevisionsInput>(PropRevisions.CreateInstance)
				.RegisterGenerator<TemplatesInput>(PropTemplates.CreateInstance)
				.RegisterGenerator<TranscludedInInput>(PropTranscludedIn.CreateInstance)
				.RegisterGenerator<AllCategoriesInput>(ListAllCategories.CreateInstance)
				.RegisterGenerator<AllDeletedRevisionsInput>(ListAllDeletedRevisions.CreateInstance)
				.RegisterGenerator<AllImagesInput>(ListAllImages.CreateInstance)
				.RegisterGenerator<AllFileUsagesInput>(ListAllLinks.CreateInstance)
				.RegisterGenerator<AllLinksInput>(ListAllLinks.CreateInstance)
				.RegisterGenerator<AllRedirectsInput>(ListAllLinks.CreateInstance)
				.RegisterGenerator<AllTransclusionsInput>(ListAllLinks.CreateInstance)
				.RegisterGenerator<AllPagesInput>(ListAllPages.CreateInstance)
				.RegisterGenerator<AllRevisionsInput>(ListAllRevisions.CreateInstance)
				.RegisterGenerator<BacklinksInput>(ListBacklinks.CreateInstance)
				.RegisterGenerator<CategoryMembersInput>(ListCategoryMembers.CreateInstance)
				.RegisterGenerator<ExternalUrlUsageInput>(ListExtUrlUsage.CreateInstance)
				.RegisterGenerator<InterwikiBacklinksInput>(ListInterwikiBacklinks.CreateInstance)
				.RegisterGenerator<LanguageBacklinksInput>(ListLanguageBacklinks.CreateInstance)
				.RegisterGenerator<PagesWithPropertyInput>(ListPagesWithProp.CreateInstance)
				.RegisterGenerator<PrefixSearchInput>(ListPrefixSearch.CreateInstance)
				.RegisterGenerator<ProtectedTitlesInput>(ListProtectedTitles.CreateInstance)
				.RegisterGenerator<QueryPageInput>(ListQueryPage.CreateInstance)
				.RegisterGenerator<RandomInput>(ListRandom.CreateInstance)
				.RegisterGenerator<RecentChangesInput>(ListRecentChanges.CreateInstance)
				.RegisterGenerator<SearchInput>(ListSearch.CreateInstance)
				.RegisterGenerator<WatchlistInput>(ListWatchlist.CreateInstance)
				.RegisterGenerator<WatchlistRawInput>(ListWatchlistRaw.CreateInstance)
				.RegisterProperty<CategoriesInput>(PropCategories.CreateInstance)
				.RegisterProperty<CategoryInfoInput>(PropCategoryInfo.CreateInstance)
				.RegisterProperty<ContributorsInput>(PropContributors.CreateInstance)
				.RegisterProperty<DeletedRevisionsInput>(PropDeletedRevisions.CreateInstance)
				.RegisterProperty<DuplicateFilesInput>(PropDuplicateFiles.CreateInstance)
				.RegisterProperty<ExternalLinksInput>(PropExternalLinks.CreateInstance)
				.RegisterProperty<FileUsageInput>(PropFileUsage.CreateInstance)
				.RegisterProperty<ImageInfoInput>(PropImageInfo.CreateInstance)
				.RegisterProperty<ImagesInput>(PropImages.CreateInstance)
				.RegisterProperty<InfoInput>(PropInfo.CreateInstance)
				.RegisterProperty<InterwikiLinksInput>(PropInterwikiLinks.CreateInstance)
				.RegisterProperty<LanguageLinksInput>(PropLanguageLinks.CreateInstance)
				.RegisterProperty<LinksInput>(PropLinks.CreateInstance)
				.RegisterProperty<LinksHereInput>(PropLinksHere.CreateInstance)
				.RegisterProperty<PagePropertiesInput>(PropPageProperties.CreateInstance)
				.RegisterProperty<RedirectsInput>(PropRedirects.CreateInstance)
				.RegisterProperty<RevisionsInput>(PropRevisions.CreateInstance)
				.RegisterProperty<TemplatesInput>(PropTemplates.CreateInstance)
				.RegisterProperty<TranscludedInInput>(PropTranscludedIn.CreateInstance);
		}
		#endregion

		#region Public Events

		/// <summary>Raised when a Captcha check is generated by the wiki.</summary>
		public event StrongEventHandler<IWikiAbstractionLayer, CaptchaEventArgs>? CaptchaChallenge;

		/// <summary>Occurs after initialization data has been loaded and processed.</summary>
		public event StrongEventHandler<IWikiAbstractionLayer, InitializationEventArgs>? Initialized;

		/// <summary>Occurs when the wiki is about to load initialization data.</summary>
		public event StrongEventHandler<IWikiAbstractionLayer, InitializationEventArgs>? Initializing;

		/// <summary>Raised when an HTTP response is received from the client.</summary>
		public event StrongEventHandler<IWikiAbstractionLayer, ResponseEventArgs>? ResponseReceived;

		/// <summary>Raised when sending a request to the client.</summary>
		public event StrongEventHandler<IWikiAbstractionLayer, RequestEventArgs>? SendingRequest;

		/// <summary>Occurs when a warning is issued by the wiki.</summary>
		public event StrongEventHandler<IWikiAbstractionLayer, WarningEventArgs>? WarningOccurred;
		#endregion

		#region Public Properties

		/// <summary>Gets the article path.</summary>
		/// <value>The article path.</value>
		public string? ArticlePath { get; private set; }

		/// <summary>Gets or sets the assert string.</summary>
		/// <value>The assert string to be used, such as "bot" or "user" for <c>assert=bot</c> or <c>assert=user</c>.</value>
		public string? Assert { get; set; }

		/// <summary>Gets the client.</summary>
		/// <value>The client.</value>
		public IMediaWikiClient Client { get; }

		/// <summary>Gets or sets the continue version.</summary>
		/// <value>The continue version.</value>
		public int ContinueVersion { get; protected internal set; }

		/// <summary>Gets or sets the most recent timestamp from the wiki, which can be used to indicate when an edit was started.</summary>
		/// <value>The current timestamp.</value>
		/// <remarks>Depending on wiki version, this will either come from setting GetTimestamp to true and getting the result from that, or from API:Info when tokens are requested or GetTimestamp is set to true.</remarks>
		public DateTime? CurrentTimestamp { get; protected internal set; }

		/// <summary>Gets or sets the custom stop check function.</summary>
		/// <value>A function which returns true if the bot should stop what it's doing.</value>
		public Func<bool>? CustomStopCheck { get; set; }

		/// <summary>Gets or sets the detected format version.</summary>
		/// <value>The detected format version.</value>
		/// <remarks>This should not normally need to be set, but is left as settable by derived classes, should customization be needed. Assumes version 2, then falls back to 1 in the event of an error message.</remarks>
		public int DetectedFormatVersion { get; protected internal set; } = 2;

		/// <summary>Gets or sets various site information flags.</summary>
		/// <value>The flags. See <see cref="SiteInfoFlags" />.</value>
		/// <remarks>This should not normally need to be set, but is left as settable by derived classes, should customization be needed.</remarks>
		public SiteInfoFlags Flags { get; protected set; }

		/// <summary>Gets the interwiki prefixes.</summary>
		/// <value>A hashset of all interwiki prefixes, to allow <see cref="PageSetRedirectItem.Interwiki"/> emulation for MW 1.24 and below.</value>
		/// <remarks>For some bizarre reason, there is no read-only collection in C# that implements the Contains method, so this is left as a writable HashSet, since it's the fastest lookup.</remarks>
		public IReadOnlyCollection<string> InterwikiPrefixes => this.interwikiPrefixes;

		/// <summary>Gets or sets the site language code.</summary>
		/// <value>The language code.</value>
		public string? LanguageCode { get; set; }

		/// <summary>Gets or sets the maximum length of get requests for a given site. Get requests that are longer than this will be sent as POST requests instead.</summary>
		/// <value>The maximum length of get requests.</value>
		public int MaximumGetLength { get; set; } = 8100;

		/// <summary>Gets or sets the maximum size of the page set.</summary>
		/// <value>The maximum size of the page set.</value>
		/// <remarks>This should not normally need to be set, as the bot will adjust automatically as needed. However, if you know in advance that you will be logged in as a user with lower limits (typically anyone who isn't a bot or admin), then you can save some overhead by lowering this to 50, rather than the default 500.</remarks>
		public int MaximumPageSetSize { get; set; } = LimitSmall2;

		/// <summary>Gets or sets the <c>maxlag</c> value to be used with the site. A value of 5 is recommended by MediaWiki, but smaller sites may want to use a different value to be more (or less) responsive to lag conditions. The lower the number, the more often the bot will pause in response to lag.</summary>
		/// <value>The maximum lag.</value>
		/// <remarks>This value has no effect on wikis that don't use a replicated database cluster. Once the internal site info has been retrieved, this will stop being emitted in the request if the site doesn't support it. See MediaWiki's <see href="https://www.mediawiki.org/wiki/Manual:Maxlag_parameter">Maxlag parameter</see> for full details.</remarks>
		public int MaxLag { get; set; } = 5;

		/// <summary>Gets or sets the module factory.</summary>
		/// <value>The module factory.</value>
		/// <seealso cref="IModuleFactory" />
		public IModuleFactory ModuleFactory { get; set; }

		/// <summary>Gets the namespace collection for the site.</summary>
		/// <value>The site's namespaces.</value>
		public IReadOnlyDictionary<int, NamespacesItem> Namespaces => this.namespaces;

		/// <summary>Gets or sets the path of index.php relative to the document root.</summary>
		/// <value>The path of index.php relative to the document root.</value>
		public string? Script { get; protected set; }

		/// <summary>Gets or sets the name of the site.</summary>
		/// <value>The name of the site.</value>
		public string? SiteName { get; protected set; }

		/// <summary>Gets or sets the detected site version.</summary>
		/// <value>The MediaWiki version for the site, expressed as an integer (i.e., MW 1.23 = 123).</value>
		/// <remarks>This should not normally need to be set, but is left as settable by derived classes, should customization be needed.</remarks>
		public int SiteVersion { get; protected set; }

		/// <summary>Gets or sets the various methods to check to see if a stop has been requested.</summary>
		/// <value>The stop methods.</value>
		public StopCheckMethods StopCheckMethods { get; set; } = StopCheckMethods.UserNameCheck | StopCheckMethods.TalkCheckQuery | StopCheckMethods.TalkCheckNonQuery;

		/// <summary>Gets or sets a value indicating whether the site supports <see href="https://www.mediawiki.org/wiki/Manual:Maxlag_parameter">maxlag checking</see>.</summary>
		/// <value><see langword="true" /> if the site supports <c>maxlag</c> checking; otherwise, <see langword="false" />.</value>
		/// <remarks>This should not normally need to be set, but is left as settable by derived classes, should customization be needed.</remarks>
		public bool SupportsMaxLag { get; protected set; } = true; // No harm in trying until we know for sure.

		/// <summary>Gets or sets the class to use as a token manager.</summary>
		/// <value>The token manager.</value>
		public ITokenManager? TokenManager { get; set; }

		/// <summary>Gets or sets the base URI used for all requests. This should be the full URI to api.php (e.g., <c>https://en.wikipedia.org/w/api.php</c>).</summary>
		/// <value>The URI to use as a base.</value>
		/// <remarks>This should normally be set only by the constructor and the <see cref="MakeUriSecure(bool)" /> routine, but is left as settable by derived classes, should customization be needed.</remarks>
		/// <seealso cref="WikiAbstractionLayer(IMediaWikiClient, Uri)" />
		public Uri Uri { get; protected set; }

		/// <summary>Gets or sets the language to use for responses from the wiki.</summary>
		/// <value>The use language.</value>
		public string? UseLanguage { get; set; }

		/// <summary>Gets or sets the user ID.</summary>
		/// <value>The user ID.</value>
		public long UserId { get; protected set; }

		/// <summary>Gets or sets the name of the current user.</summary>
		/// <value>The name of the current user.</value>
		public string? UserName { get; protected set; }

		/// <summary>Gets or sets a value indicating whether to use UTF-8 encoding for responses.</summary>
		/// <value><see langword="true" /> to use UTF-8; otherwise, <see langword="false" />. Defaults to <see langword="true" />.</value>
		public bool Utf8 { get; set; } = true;

		/// <summary>Gets a list of all warnings.</summary>
		/// <value>The warnings.</value>
		public IReadOnlyList<ErrorItem> Warnings => this.warnings;
		#endregion

		#region Protected Internal Properties

		/// <summary>Gets or sets a value indicating whether to break recursion during the AfterSubmit cycle.</summary>
		/// <value><see langword="true" /> to skip the AfterSubmit cycle, thus breaking recursion; otherwise, <see langword="false" />.</value>
		/// <remarks>Custom stop checks might rely on calls to additional modules in order to determine whether the bot should stop. Since these each have their own AfterSubmit process, the entire check would become recursive. The AfterSubmit routine manages this variable to ensure that stop checks are only performed at the top-most level. When set to true, the routine returns immediately without performing any additional stop checks.</remarks>
		protected internal bool BreakRecursionAfterSubmit { get; set; }
		#endregion

		#region Private Properties
		private ITokenManager CheckedTokenManager => this.TokenManager ?? throw this.notInitialized;
		#endregion

		#region Public Static Methods

		/// <summary>The default page factory when none is provided.</summary>
		/// <param name="ns">The namespace.</param>
		/// <param name="title">The title.</param>
		/// <param name="pageId">The page identifier.</param>
		/// <returns>A factory methods which creates a new PageItem.</returns>
		public static PageItem DefaultPageFactory(int ns, string title, long pageId) => new PageItem(ns, title, pageId);
		#endregion

		#region Public Methods

		/// <summary>Gets the full path for an article given its page name.</summary>
		/// <param name="pageName">The name of the page.</param>
		/// <returns>An string representing either an absolute or relative URI to the article.</returns>
		/// <remarks>This does not return a Uri object because the article path may be relative, which is not supported by the C# Uri class. Although this function could certainly be made to provide a fixed Uri, that might not be what the caller wants, so the caller is left to interpret the result value as they wish.</remarks>
		public Uri GetArticlePath(string pageName)
		{
			ThrowNull(pageName, nameof(pageName));
			var articlePath = this.ArticlePath ?? throw this.notInitialized;
			return new Uri(articlePath.Replace("$1", pageName.Replace(' ', '_')));
		}

		/// <summary>Makes the URI secure.</summary>
		/// <param name="https">If set to <see langword="true" />, forces the URI to be a secure URI (https://); if false, forces it to be insecure (http://).</param>
		public void MakeUriSecure(bool https)
		{
			var urib = new UriBuilder(this.Uri)
			{
				Scheme = https ? "https" : "http",
			};
			this.Uri = urib.Uri;
		}

		/// <summary>Runs the continuable query specified by the input.</summary>
		/// <param name="input">The input.</param>
		/// <remarks>This function is used internally, but also made available externally for special situations. The caller is responsible for deciding whether any given query is continuable.</remarks>
		public void RunContinuableQuery(QueryInput input) => new ActionQuery(this).SubmitContinued(input);

		/// <summary>Runs the query specified based directly on the input module.</summary>
		/// <typeparam name="TInput">The input type for the module.</typeparam>
		/// <typeparam name="TOutput">The output type for the module.</typeparam>
		/// <param name="module">The input module.</param>
		/// <returns>The module output.</returns>
		/// <remarks>This function is used internally, but also made available externally for special situations.</remarks>
		public TOutput RunModuleQuery<TInput, TOutput>(QueryModule<TInput, TOutput> module)
			where TInput : class
			where TOutput : class
		{
			ThrowNull(module, nameof(module));
			var input = new QueryInput(module);
			var query = new ActionQuery(this);
			if (module is IContinuableQueryModule)
			{
				query.SubmitContinued(input);
			}
			else
			{
				query.Submit(input);
			}

			return module.Output;
		}

		/// <summary>Runs the pageset query specified by the input.</summary>
		/// <param name="input">The input.</param>
		/// <remarks>This function is used internally, but also made available externally for special situations.</remarks>
		/// <returns>A list of <see cref="PageItem"/>s.</returns>
		public PageSetResult<PageItem> RunPageSetQuery(QueryInput input) => this.RunPageSetQuery(input, DefaultPageFactory);

		/// <summary>Runs the pageset query specified by the input.</summary>
		/// <param name="input">The input.</param>
		/// <param name="pageFactory">The factory method to use to generate PageItem derivatives.</param>
		/// <remarks>This function is used internally, but also made available externally for special situations.</remarks>
		/// <returns>A list of <see cref="PageItem"/>s of the specified underlying type.</returns>
		public PageSetResult<PageItem> RunPageSetQuery(QueryInput input, TitleCreator<PageItem> pageFactory) => new ActionQuery(this, pageFactory).SubmitPageSet(input);

		/// <summary>Converts the given request into an HTML request and submits it to the site.</summary>
		/// <param name="request">The request.</param>
		/// <returns>The site's response to the request.</returns>
		public string? SendRequest(Request request)
		{
			ThrowNull(request, nameof(request));
			this.OnSendingRequest(new RequestEventArgs(request));
			string? response;
			switch (request.Type)
			{
				case RequestType.Post:
					response = this.Client.Post(request.Uri, RequestVisitorUrl.Build(request));
					break;
				case RequestType.PostMultipart:
					var result = RequestVisitorMultipart.Build(request);
					response = this.Client.Post(request.Uri, result.ContentType, result.Data);
					break;
				default:
					var query = RequestVisitorUrl.Build(request);
					var urib = new UriBuilder(request.Uri) { Query = query };
					response = urib.Uri.OriginalString.Length < this.MaximumGetLength ? this.Client.Get(urib.Uri) : this.Client.Post(request.Uri, RequestVisitorUrl.Build(request));
					break;
			}

			if (response != null)
			{
				this.OnResponseReceived(new ResponseEventArgs(response));
			}

			return response;
		}
		#endregion

		#region IWikiAbstractionLayer Support Methods

		/// <summary>Adds a warning to the warning list.</summary>
		/// <param name="code">The code returned by the wiki.</param>
		/// <param name="info">The informative text returned by the wiki.</param>
		public void AddWarning(string code, string info)
		{
			var warning = new ErrorItem(code, info);
			this.warnings.Add(warning);
			this.OnWarningOccurred(new WarningEventArgs(warning));
		}

		/// <summary>Clears the warning list.</summary>
		public void ClearWarnings() => this.warnings.Clear();

		/// <summary>Initializes any needed information without trying to login.</summary>
		public void Initialize()
		{
			/*
			Namespaces is used for:
				* ClearHasMessage < 1.24,
				* fixing a bug in API:Search < 1.25,
				* converting WatchItem's Title to a traditional ITitle value.
			InterwikiMap is only required to emulate PageSet redirects' tointerwiki property for < 1.25.
			*/
			var eventArgs = new InitializationEventArgs(new SiteInfoInput() { Properties = NeededSiteInfo }, null);
			this.OnInitializing(eventArgs);

			// Create input from return values in eventArgs
			var siteInfoInput = new SiteInfoInput()
			{
				FilterLocalInterwiki = eventArgs.FilterLocalInterwiki,
				InterwikiLanguageCode = eventArgs.InterwikiLanguageCode,
				Properties = eventArgs.Properties,
				ShowAllDatabases = eventArgs.ShowAllDatabases,
				ShowNumberInGroup = eventArgs.ShowNumberInGroup
			};

			var infoModule = new MetaSiteInfo(this, siteInfoInput);
			var userModule = new MetaUserInfo(this, new UserInfoInput());
			this.RunContinuableQuery(new QueryInput(infoModule, userModule));

			this.UserId = userModule.Output.Id;
			this.UserName = userModule.Output.Name;

			var siteInfo = infoModule.Output;

			// General
			this.Flags = siteInfo.Flags;
			this.LanguageCode = siteInfo.Language;
			this.SiteName = siteInfo.SiteName;
			this.Script = siteInfo.Script;
			var path = siteInfo.ArticlePath;
			if (path.StartsWith("/", StringComparison.Ordinal))
			{
				var repl = path.Substring(0, path.IndexOf("$1", StringComparison.Ordinal));
				var articleBaseIndex = siteInfo.BasePage.IndexOf(repl, StringComparison.Ordinal);
				if (articleBaseIndex < 0)
				{
					articleBaseIndex = siteInfo.BasePage.IndexOf("/", siteInfo.BasePage.IndexOf("//", StringComparison.Ordinal) + 2, StringComparison.Ordinal);
				}

				path = siteInfo.BasePage.Substring(0, articleBaseIndex) + path;
			}

			this.ArticlePath = path;
			var versionFudged = Regex.Replace(siteInfo.Generator, @"[^0-9\.]", ".").TrimStart(TextArrays.Period);
			var versionSplit = versionFudged.Split(TextArrays.Period);
			var siteVersion = int.Parse(versionSplit[0], CultureInfo.InvariantCulture) * 100 + int.Parse(versionSplit[1], CultureInfo.InvariantCulture);
			this.SiteVersion = siteVersion;

			// Namespaces
			this.namespaces.Clear();
			foreach (var ns in siteInfo.Namespaces)
			{
				this.namespaces.Add(ns.Id, ns);
			}

			// Interwiki
			this.interwikiPrefixes.Clear();
			foreach (var interwiki in siteInfo.InterwikiMap)
			{
				this.interwikiPrefixes.Add(interwiki.Prefix);
			}

			// DbReplLag
			this.SupportsMaxLag = siteInfo.LagInfo?.Count > 0 && siteInfo.LagInfo[0].Lag != -1;

			// Other (not SiteInfo-related)
			if (this.TokenManager == null)
			{
				this.TokenManager =
					siteVersion >= TokenManagerMeta.MinimumVersion ? new TokenManagerMeta(this) :
					siteVersion >= TokenManagerAction.MinimumVersion ? new TokenManagerAction(this) :
					new TokenManagerOriginal(this) as ITokenManager;
			}
			else
			{
				// Token Manager can be non-null if we're using the site after logging out (possibly to log in again as someone else). It's pointless to reinitialize it in that case.
				this.TokenManager.Clear();
			}

			if (this.ContinueVersion == 0)
			{
				this.ContinueVersion = siteVersion >= ContinueModule2.MinimumVersion ? 2 : 1;
			}

			this.OnInitialized(new InitializationEventArgs(siteInfoInput, siteInfo));
		}

		/// <summary>Determines whether the API is enabled (even if read-only) on the current wiki.</summary>
		/// <returns><see langword="true" /> if the interface is enabled; otherwise, <see langword="false" />.</returns>
		/// <remarks>This function will normally need to communicate with the wiki to determine the return value. Since that consumes significantly more time than a simple property check, it's implemented as a function rather than a property.</remarks>
		public bool IsEnabled()
		{
			if (this.SiteVersion > 0)
			{
				// In the unusual case that we're already setup, obviously the API is enabled.
				return true;
			}

			var stopCheck = this.StopCheckMethods;
			try
			{
				this.StopCheckMethods = StopCheckMethods.None;
				new ActionQuery(this).Submit(new QueryInput());

				return true;
			}
			catch (WebException e)
			{
				// Internal Server Error is a valid possibility from MW 1.18 on, so ignore that, but throw if we get anything else.
				var response = (HttpWebResponse)e.Response;
				if (response.StatusCode != HttpStatusCode.InternalServerError)
				{
					throw;
				}
			}
			catch (WikiException e)
			{
				if (e.Code != ApiDisabledCode)
				{
					throw;
				}
			}
			finally
			{
				// Unlikely to come into play if the check fails, but just to be safe, restore it either way.
				this.StopCheckMethods = stopCheck;
			}

			return false;
		}
		#endregion

		#region IWikiAbstractionLayer Methods

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Allcategories">Allcategories</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of categories.</returns>
		public IReadOnlyList<AllCategoriesItem> AllCategories(AllCategoriesInput input) => this.RunListQuery(new ListAllCategories(this, input));

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Alldeletedrevisions">Alldeletecrevisions</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of deleted revisions.</returns>
		public IReadOnlyList<AllRevisionsItem> AllDeletedRevisions(AllDeletedRevisionsInput input) => this.RunListQuery(new ListAllDeletedRevisions(this, input));

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Alllinks">Allfileusages</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of file usage links.</returns>
		public IReadOnlyList<AllLinksItem> AllFileUsages(AllFileUsagesInput input) => this.RunListQuery(new ListAllLinks(this, input));

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Allimages">Allimages</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of images.</returns>
		public IReadOnlyList<AllImagesItem> AllImages(AllImagesInput input) => this.RunListQuery(new ListAllImages(this, input));

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Alllinks">Alllinks</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of links.</returns>
		/// <exception cref="ArgumentException">Thrown if <paramref name="input" />.LinkType is set to None or contains only numeric values outside the flag values.</exception>
		public IReadOnlyList<AllLinksItem> AllLinks(AllLinksInput input) => this.RunListQuery(new ListAllLinks(this, input));

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Allmessages">Allmessages</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <remarks>Prior to MediaWiki 1.26, NormalizedName will be derived automatically from the name of the message. If the first letter is upper-case, it will be converted to lower-case using the LanguageCode, if recognized by Windows, or the CurrentCulture if not.</remarks>
		/// <returns>A list of messages.</returns>
		public IReadOnlyList<AllMessagesItem> AllMessages(AllMessagesInput input) => this.RunListQuery(new MetaAllMessages(this, input));

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Allpages">Allpages</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of page titles.</returns>
		public IReadOnlyList<WikiTitleItem> AllPages(AllPagesInput input) => this.RunListQuery(new ListAllPages(this, input));

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Alllinks">Allredirects</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of redirect links.</returns>
		public IReadOnlyList<AllLinksItem> AllRedirects(AllRedirectsInput input) => this.RunListQuery(new ListAllLinks(this, input));

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Allrevisions">Allrevisions</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of revisions.</returns>
		public IReadOnlyList<AllRevisionsItem> AllRevisions(AllRevisionsInput input) => this.RunListQuery(new ListAllRevisions(this, input));

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Alllinks">Alltransclusions</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of transclusions (as links).</returns>
		public IReadOnlyList<AllLinksItem> AllTransclusions(AllTransclusionsInput input) => this.RunListQuery(new ListAllLinks(this, input));

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Allusers">Allusers</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of users.</returns>
		public IReadOnlyList<AllUsersItem> AllUsers(AllUsersInput input) => this.RunListQuery(new ListAllUsers(this, input));

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Backlinks">Backlinks</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of links.</returns>
		public IReadOnlyList<BacklinksItem> Backlinks(BacklinksInput input)
		{
			ThrowNull(input, nameof(input));
			var modules = new List<ListBacklinks>();
			foreach (var type in input.LinkTypes.GetUniqueFlags())
			{
				modules.Add(new ListBacklinks(this, new BacklinksInput(input, type)));
			}

			this.RunContinuableQuery(new QueryInput(modules));
			var output = new HashSet<BacklinksItem>(new BacklinksOutputComparer());
			foreach (var module in modules)
			{
				output.UnionWith(module.Output);
			}

			return output.AsReadOnlyList();
		}

		/// <summary>Blocks a user using the <see href="https://www.mediawiki.org/wiki/API:Block">Block</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the block.</returns>
		public BlockResult Block(BlockInput input)
		{
			ThrowNull(input, nameof(input));
			input.Token ??= this.GetSessionToken(TokensInput.Csrf);
			return new ActionBlock(this).Submit(input);
		}

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Blocks">Blocks</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of blocks.</returns>
		public IReadOnlyList<BlocksResult> Blocks(BlocksInput input) => this.RunListQuery(new ListBlocks(this, input));

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Categorymembers">Categorymembers</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of category members.</returns>
		public IReadOnlyList<CategoryMembersItem> CategoryMembers(CategoryMembersInput input) => this.RunListQuery(new ListCategoryMembers(this, input));

		/// <summary>Checks a token using the <see href="https://www.mediawiki.org/wiki/API:Checktoken">Checktoken</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the checked token.</returns>
		public CheckTokenResult CheckToken(CheckTokenInput input) => new ActionCheckToken(this).Submit(input);

		/// <summary>Clears the user's "has message" flag using the <see href="https://www.mediawiki.org/wiki/API:Clearhasmsg">Clearhasmsg</see> API module or by visiting the user's talk page on wikis below version 1.24.</summary>
		/// <returns>Whether the attempt was successful.</returns>
		public bool ClearHasMessage()
		{
			try
			{
				return new ActionClearHasMsg(this).Submit(NullObject.Null).Result == "success";
			}
			catch (NotSupportedException)
			{
			}

			var index = this.GetArticlePath(this.Namespaces[MediaWikiNamespaces.UserTalk].Name + ":" + this.UserName);
			return !string.IsNullOrEmpty(this.Client.Get(index));
		}

		/// <summary>Compares two revisions or pages using the <see href="https://www.mediawiki.org/wiki/API:Compare">Compare</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the comparison.</returns>
		public CompareResult Compare(CompareInput input) => new ActionCompare(this).Submit(input);

		/// <summary>Creates an account using the <see href="https://www.mediawiki.org/wiki/API:Createaccount">Createaccount</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the account created.</returns>
		public CreateAccountResult CreateAccount(CreateAccountInput input)
		{
			var create = new ActionCreateAccount(this);
			CreateAccountResult retval;
			var retries = 3; // We potentially need to get the token, submit the token, and respond to a Captcha just for a single request.
			bool doCaptcha;
			do
			{
				retries--;
				retval = create.Submit(input);
				doCaptcha = false;
				if (create.CaptchaData.Count > 0)
				{
					var eventArgs = new CaptchaEventArgs(create.CaptchaData, create.CaptchaSolution);
					this.OnCaptchaChallenge(eventArgs);
					if (eventArgs.CaptchaSolution.Count > 0)
					{
						doCaptcha = true;
					}
				}
			}
			while (retries > 0 && (doCaptcha || retval.Result == "NeedToken"));

			// Unlike Login, it is not necessarily a critical event if user creation fails, so just return the result regardless of success.
			return retval;
		}

		/// <summary>Deletes a page using the <see href="https://www.mediawiki.org/wiki/API:Delete">Delete</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the deletion.</returns>
		public DeleteResult Delete(DeleteInput input)
		{
			ThrowNull(input, nameof(input));
			input.Token ??= this.GetSessionToken(TokensInput.Csrf);
			return new ActionDelete(this).Submit(input);
		}

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Deletedrevisions">Deletedrevisions</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of deleted revisions.</returns>
		public IReadOnlyList<DeletedRevisionsItem> DeletedRevisions(ListDeletedRevisionsInput input) => this.RunListQuery(new ListDeletedRevs(this, input));

		/// <summary>Downloads the specified resource (typically, a Uri) to a file.</summary>
		/// <param name="input">The input parameters.</param>
		/// <remarks>This is not part of the API, but since Upload is, it makes sense to provide its counterpart so the end-user is not left accessing Client directly.</remarks>
		public void Download(DownloadInput input)
		{
			ThrowNull(input, nameof(input));
			var uri = new Uri(input.Resource);
			this.Client.DownloadFile(uri, input.FileName);
		}

		/// <summary>Edits a page using the <see href="https://www.mediawiki.org/wiki/API:Edit">Edit</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the edit.</returns>
		public EditResult Edit(EditInput input)
		{
			ThrowNull(input, nameof(input));
			input.Token ??= this.GetSessionToken(TokensInput.Csrf);
			var edit = new ActionEdit(this);
			EditResult retval;
			bool doCaptcha;
			var retry = false; // Only one edit and one captcha check so we're not looping in case of captcha failure.
			do
			{
				doCaptcha = false;
				retval = edit.Submit(input);
				if (edit.CaptchaData.Count > 0)
				{
					var eventArgs = new CaptchaEventArgs(edit.CaptchaData, edit.CaptchaSolution);
					this.OnCaptchaChallenge(eventArgs);
					if (eventArgs.CaptchaSolution.Count > 0)
					{
						retry = !retry;
						doCaptcha = true;
					}
				}
			}
			while (doCaptcha && retry);

			return retval;
		}

		/// <summary>Emails a user using the <see href="https://www.mediawiki.org/wiki/API:Emailuser">Emailuser</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the e-mail that was sent.</returns>
		public EmailUserResult EmailUser(EmailUserInput input)
		{
			ThrowNull(input, nameof(input));
			input.Token ??= this.GetSessionToken(TokensInput.Csrf);
			return new ActionEmailUser(this).Submit(input);
		}

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Expandtemplates">Expandtemplates</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the expanded templates.</returns>
		public ExpandTemplatesResult ExpandTemplates(ExpandTemplatesInput input) => new ActionExpandTemplates(this).Submit(input);

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Externalurlusage">Externalurlusage</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of external URLs and the pages they're used on.</returns>
		public IReadOnlyList<ExternalUrlUsageItem> ExternalUrlUsage(ExternalUrlUsageInput input) => this.RunListQuery(new ListExtUrlUsage(this, input));

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Feedcontributions">Feedcontributions</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>The raw XML of the Contributions RSS feed.</returns>
		public string FeedContributions(FeedContributionsInput input) => new ActionFeedContributions(this).Submit(input).Result;

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Feedrecentchanges">Feedrecentchanges</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>The raw XML of the Recent Changes RSS feed.</returns>
		public string FeedRecentChanges(FeedRecentChangesInput input) => new ActionFeedRecentChanges(this).Submit(input).Result;

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Feedwatchlist">Feedwatchlist</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>The raw XML of the Watchlist RSS feed.</returns>
		public string FeedWatchlist(FeedWatchlistInput input) => new ActionFeedWatchlist(this).Submit(input).Result;

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Filearchive">Filearchive</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of file archives.</returns>
		public IReadOnlyList<FileArchiveItem> FileArchive(FileArchiveInput input) => this.RunListQuery(new ListFileArchive(this, input));

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Filerepoinfo">Filerepoinfo</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of information for each repository.</returns>
		public IReadOnlyList<FileRepositoryInfoItem> FileRepositoryInfo(FileRepositoryInfoInput input) => this.RunListQuery(new MetaFileRepoInfo(this, input));

		/// <summary>Reverts a file to an older version using the <see href="https://www.mediawiki.org/wiki/API:Filerevert">Filerevert</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the file reversion.</returns>
		public FileRevertResult FileRevert(FileRevertInput input)
		{
			ThrowNull(input, nameof(input));
			input.Token ??= this.GetSessionToken(TokensInput.Csrf);
			return new ActionFileRevert(this).Submit(input);
		}

		/// <summary>Gets help information using the <see href="https://www.mediawiki.org/wiki/API:Help">Help</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>The API help for the module(s) requested.</returns>
		public HelpResult Help(HelpInput input) => new ActionHelp(this).Submit(input);

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Imagerotate">Imagerotate</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of page titles with image rotation information.</returns>
		public PageSetResult<ImageRotateItem> ImageRotate(ImageRotateInput input)
		{
			ThrowNull(input, nameof(input));
			input.Token ??= this.GetSessionToken(TokensInput.Csrf);
			return new ActionImageRotate(this).SubmitPageSet(input);
		}

		/// <summary>Imports pages into a wiki. Correspondes to the <see href="https://www.mediawiki.org/wiki/API:Import">Import</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of page titles with import information.</returns>
		public IReadOnlyList<ImportItem> Import(ImportInput input)
		{
			ThrowNull(input, nameof(input));
			input.Token ??= this.GetSessionToken(TokensInput.Csrf);
			return new ActionImport(this).Submit(input);
		}

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Iwbacklinks">Iwbacklinks</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of page titles with interwiki backlink information.</returns>
		public IReadOnlyList<InterwikiBacklinksItem> InterwikiBacklinks(InterwikiBacklinksInput input) => this.RunListQuery(new ListInterwikiBacklinks(this, input));

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Langbacklinks">Langbacklinks</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of page titles with language backlink information.</returns>
		public IReadOnlyList<LanguageBacklinksItem> LanguageBacklinks(LanguageBacklinksInput input) => this.RunListQuery(new ListLanguageBacklinks(this, input));

		/// <summary>Loads page information. Incorporates the various API <see href="https://www.mediawiki.org/wiki/API:Properties">property</see> modules.</summary>
		/// <param name="pageSetInput">A pageset input which specifies a list of page titles, page IDs, revision IDs, or a generator.</param>
		/// <param name="propertyInputs"><para>A collection of any combination of property inputs. Built-in property inputs include: <see cref="CategoriesInput" />, <see cref="CategoryInfoInput" />, <see cref="ContributorsInput" />, <see cref="DeletedRevisionsInput" />, <see cref="DuplicateFilesInput" />, <see cref="ExternalLinksInput" />, <see cref="FileUsageInput" />, <see cref="ImageInfoInput" />, <see cref="ImagesInput" />, <see cref="InfoInput" />, <see cref="InterwikiLinksInput" />, <see cref="LanguageLinksInput" />, <see cref="LinksHereInput" />, <see cref="PagePropertiesInput" />, <see cref="RedirectsInput" />, <see cref="RevisionsInput" />, <see cref="StashImageInfoInput" />, and <see cref="TranscludedInInput" />.</para>
		/// <para>A typical, simple collection would include an InfoInput and a RevisionsInput, which would fetch basic information about the page, along with the latest revision.</para></param>
		/// <returns>A list of pages based on the pageSetInput parameter with the information for each of the property inputs.</returns>
		public PageSetResult<PageItem> LoadPages(QueryPageSetInput pageSetInput, IEnumerable<IPropertyInput> propertyInputs)
		{
			ThrowNull(pageSetInput, nameof(pageSetInput));
			ThrowNull(propertyInputs, nameof(propertyInputs));
			var propertyModules = this.ModuleFactory.CreateModules(propertyInputs);
			return this.RunPageSetQuery(new QueryInput(pageSetInput, propertyModules));
		}

		/// <summary>Loads page information. Incorporates the various API <see href="https://www.mediawiki.org/wiki/API:Properties">property</see> modules.</summary>
		/// <param name="pageSetInput">A pageset input which specifies a list of page titles, page IDs, revision IDs, or a generator.</param>
		/// <param name="propertyInputs"><para>A collection of any combination of property inputs. Built-in property inputs include: <see cref="CategoriesInput" />, <see cref="CategoryInfoInput" />, <see cref="ContributorsInput" />, <see cref="DeletedRevisionsInput" />, <see cref="DuplicateFilesInput" />, <see cref="ExternalLinksInput" />, <see cref="FileUsageInput" />, <see cref="ImageInfoInput" />, <see cref="ImagesInput" />, <see cref="InfoInput" />, <see cref="InterwikiLinksInput" />, <see cref="LanguageLinksInput" />, <see cref="LinksHereInput" />, <see cref="PagePropertiesInput" />, <see cref="RedirectsInput" />, <see cref="RevisionsInput" />, <see cref="StashImageInfoInput" />, and <see cref="TranscludedInInput" />.</para>
		/// <para>A typical, simple collection would include an InfoInput and a RevisionsInput, which would fetch basic information about the page, along with the latest revision.</para></param>
		/// <param name="pageFactory">A factory method which creates an object derived from PageItem.</param>
		/// <returns>A list of pages based on the <paramref name="pageSetInput" /> parameter with the information determined by each of the property inputs.</returns>
		public PageSetResult<PageItem> LoadPages(QueryPageSetInput pageSetInput, IEnumerable<IPropertyInput> propertyInputs, TitleCreator<PageItem> pageFactory)
		{
			ThrowNull(pageSetInput, nameof(pageSetInput));
			ThrowNull(propertyInputs, nameof(propertyInputs));
			ThrowNull(pageFactory, nameof(pageFactory));
			var propertyModules = this.ModuleFactory.CreateModules(propertyInputs);
			return this.RunPageSetQuery(new QueryInput(pageSetInput, propertyModules), pageFactory);
		}

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Logevents">Logevents</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of log events. The specific class used for each event will vary depending on the event itself.</returns>
		public IReadOnlyList<LogEventsItem> LogEvents(LogEventsInput input) => this.RunListQuery(new ListLogEvents(this, input));

		/// <summary>Logs the user in using the <see href="https://www.mediawiki.org/wiki/API:Login">Login</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the login.</returns>
		public LoginResult Login(LoginInput input)
		{
			ThrowNull(input, nameof(input));
			if (this.SiteVersion == 0)
			{
				this.Initialize();
			}

			if (string.IsNullOrEmpty(input.UserName))
			{
				this.CheckedTokenManager.Clear();
				return LoginResult.EditingAnonymously;
			}

			var userNameSplit = input.UserName.Split(TextArrays.At);
			var botPasswordName = userNameSplit[userNameSplit.Length - 1];

			// Both checks are necessary because user names can legitimately contain @ signs.
			if (this.UserName == input.UserName || this.UserName == botPasswordName)
			{
				return LoginResult.AlreadyLoggedIn(this.UserId, this.UserName);
			}

			// Second logins are not allowed without first logging out in later versions of the API.
			this.Logout(false); // Explicitly call non-clearing logout or the rest of the method will fail.

			if (this.SiteVersion >= 127)
			{
				input.Token ??= this.GetSessionToken(TokensInput.Login);
				this.TokenManager!.Clear();
			}

			var assert = this.Assert;
			this.Assert = null;
			var retries = 4; // Allow up to four retries in case of throttling, plus the NeedToken portion of the request.
			LoginResult output;
			do
			{
				var login = new ActionLogin(this);
				output = login.Submit(input);
				switch (output.Result)
				{
					case "NeedToken":
						// Counts as a retry in case we're stuck in a NeedToken loop, which can happen if cookies are not handled properly or in some other rare circumstances.
						input.Token = output.Token;
						retries--;
						break;
					case "Success":
						this.UserName = output.User;
						this.UserId = output.UserId;
						this.Client.SaveCookies();
						retries = 0;
						break;
					case "Throttled":
						var delayTime = output.WaitTime.Add(TimeSpan.FromMilliseconds(500)); // Add an extra 500 milliseconds because in practice, waiting the exact amount of time often failed due to slight variations in time between the wiki server and the local computer.
						if (this.Client.RequestDelay(delayTime, DelayReason.LoginThrottled, "Login throttled"))
						{
							retries--;
						}
						else
						{
							retries = 0;
						}

						break;
					default:
						// Retrying will not help in any other case.
						retries = 0;
						break;
				}
			}
			while (retries > 0);

			this.Assert = assert;
			return output;
		}

		/// <summary>Logs the user out using the <see href="https://www.mediawiki.org/wiki/API:Logout">Logout</see> API module.</summary>
		/// <remarks>This call does not clear site data, since the user will most likely stop using the Site object after logging out anyway. This could also be a logout due to detecting that the wrong user is currently logged in, in which case most of the site information is still valid, and the user may even be intentionally access the site anonymously. Since the user's intent is unknown, it is safer not to assume it.</remarks>
		public void Logout() => this.Logout(false);

		/// <summary>Logs the user out using the <see href="https://www.mediawiki.org/wiki/API:Logout">Logout</see> API module.</summary>
		/// <param name="clear">Whether to clear all site data or not. If you intend to keep using the site even after logging out, set this to <see langword="false"/>; use <see langword="true"/> to clear all site-specific data, such as namespaces and site name.</param>
		public void Logout(bool clear)
		{
			if (this.UserId != 0)
			{
				var input = new LogoutInput();
				if (this.SiteVersion >= 134)
				{
					input.Token = this.GetSessionToken(TokensInput.Csrf);
				}

				new ActionLogout(this).Submit(input);
				this.Client.SaveCookies();

				// These are the only things that we absolutely must clear when logging out, since the site could conceivably still be used afterwards, even though it probably shouldn't be.
				this.UserId = 0;
				this.UserName = null;
			}

			if (clear)
			{
				this.Clear();
			}
			else
			{
				this.TokenManager?.Clear();
			}
		}

		/// <summary>Adds, removes, activates, or deactivates a page tag using the <see href="https://www.mediawiki.org/wiki/API:Managetags">Managetags</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the altered tag.</returns>
		public ManageTagsResult ManageTags(ManageTagsInput input)
		{
			ThrowNull(input, nameof(input));
			input.Token ??= this.GetSessionToken(TokensInput.Csrf);
			return new ActionManageTags(this).Submit(input);
		}

		/// <summary>Merges the history of two pages using the <see href="https://www.mediawiki.org/wiki/API:Mergehistory">Mergehistory</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the merge.</returns>
		public MergeHistoryResult MergeHistory(MergeHistoryInput input)
		{
			ThrowNull(input, nameof(input));
			input.Token ??= this.GetSessionToken(TokensInput.Csrf);
			return new ActionMergeHistory(this).Submit(input);
		}

		/// <summary>Moves a page, and optionally, it's talk/sub-pages using the <see href="https://www.mediawiki.org/wiki/API:Move">Move</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of results for each page, subpage, or talk page moved, including errors that may indicate only partial success.</returns>
		/// <remarks>Due to the fact that this method can generate multiple errors, any errors returned here will not be raised as exceptions. Results should instead be scanned for errors, and acted upon accordingly.</remarks>
		public MoveResult Move(MoveInput input)
		{
			ThrowNull(input, nameof(input));
			input.Token ??= this.GetSessionToken(TokensInput.Csrf);
			return new ActionMove(this).Submit(input);
		}

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Opensearch">Opensearch</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of open search results.</returns>
		/// <seealso cref="PrefixSearch" />
		/// <seealso cref="Search" />
		public IReadOnlyList<OpenSearchItem> OpenSearch(OpenSearchInput input)
		{
			ThrowNull(input, nameof(input));
			return new ActionOpenSearch(this).Submit(input);
		}

		/// <summary>Sets one or more options using the <see href="https://www.mediawiki.org/wiki/API:Options">Options</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <remarks>The MediaWiki return value is hard-coded to "success" and is therefore useless, so this is a void function.</remarks>
		public void Options(OptionsInput input)
		{
			ThrowNull(input, nameof(input));

			// Set input Token, even though it's not used directly, so this behaves like other routines.
			input.Token ??= this.GetSessionToken(TokensInput.Csrf);
			var change = new List<string>();
			var internalInput = new OptionsInputInternal(input.Token, change);
			if (input.Change != null)
			{
				var singleItems = new Dictionary<string, string?>();
				string? lastKey = null;
				foreach (var changeItem in input.Change)
				{
					if (this.SiteVersion < 128 && (changeItem.Key.Contains("|") || (changeItem.Value?.Contains("|") ?? false)))
					{
						singleItems.Add(changeItem.Key, changeItem.Value);
						lastKey = changeItem.Key;
					}
					else
					{
						change.Add(string.Concat(changeItem.Key, "=", changeItem.Value));
					}
				}

				if (lastKey != null)
				{
					// Don't send more requests than necessary - add final option to internalInput instead.
					internalInput.OptionName = lastKey;
					internalInput.OptionValue = singleItems[lastKey];
					singleItems.Remove(lastKey);
					foreach (var value in singleItems)
					{
						var singleInput = new OptionsInputInternal(input.Token, value.Key, value.Value);
						new ActionOptions(this).Submit(singleInput);
					}
				}
			}

			new ActionOptions(this).Submit(internalInput);
		}

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Pagepropnames">Pagepropnames</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of proerty names.</returns>
		public IReadOnlyList<string> PagePropertyNames(PagePropertyNamesInput input) => this.RunListQuery(new ListPagePropertyNames(this, input));

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Pageswithprop">Pageswithprop</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of page titles along with the value of the property on that page.</returns>
		public IReadOnlyList<PagesWithPropertyItem> PagesWithProperty(PagesWithPropertyInput input) => this.RunListQuery(new ListPagesWithProp(this, input));

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Paraminfo">Paraminfo</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A dictionary of parameter information with the module name as the key.</returns>
		public IReadOnlyDictionary<string, ParameterInfoItem> ParameterInfo(ParameterInfoInput input) => new ActionParamInfo(this).Submit(input);

		/// <summary>Parses custom text, a page, or a revision using the <see href="https://www.mediawiki.org/wiki/API:Parse">Parse</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>The result of the parse.</returns>
		public ParseResult Parse(ParseInput input) => new ActionParse(this).Submit(input);

		/// <summary>Patrols a recent change or revision using the <see href="https://www.mediawiki.org/wiki/API:Patrol">Patrol</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>The patrolled page information along with the Recent Changes ID.</returns>
		public PatrolResult Patrol(PatrolInput input)
		{
			ThrowNull(input, nameof(input));
			input.Token ??= this.GetSessionToken(TokensInput.Patrol);
			return new ActionPatrol(this).Submit(input);
		}

		/// <summary>Searches one or more namespaces for page titles prefixed by the given characters. Unlike AllPages, the search pattern is not considered rigid, and may include parsing out definite articles or similar modifications using the <see href="https://www.mediawiki.org/wiki/API:Prefixsearch">Prefixsearch</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of page titles matching the prefix.</returns>
		/// <seealso cref="OpenSearch" />
		/// <seealso cref="Search" />
		public IReadOnlyList<WikiTitleItem> PrefixSearch(PrefixSearchInput input) => this.RunListQuery(new ListPrefixSearch(this, input));

		/// <summary>Protects a page using the <see href="https://www.mediawiki.org/wiki/API:Protect">Protect</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the protection applied.</returns>
		public ProtectResult Protect(ProtectInput input)
		{
			ThrowNull(input, nameof(input));
			input.Token ??= this.GetSessionToken(TokensInput.Csrf);
			return new ActionProtect(this).Submit(input);
		}

		/// <summary>Retrieves page titles that are creation-protected using the <see href="https://www.mediawiki.org/wiki/API:Protectedtitles">Protectedtitles</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of creation-protected articles.</returns>
		public IReadOnlyList<ProtectedTitlesItem> ProtectedTitles(ProtectedTitlesInput input) => this.RunListQuery(new ListProtectedTitles(this, input));

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Purge">Purge</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of pages that were purged, along with information about the purge for each page.</returns>
		public PageSetResult<PurgeItem> Purge(PurgeInput input) => new ActionPurge(this).SubmitPageSet(input);

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Querypage">Querypage</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of pages titles and, when available, the related value. Other fields will be returned as a set of name-value pairs in the <see cref="QueryPageItem.DatabaseResult" /> dictionary.</returns>
		public QueryPageResult QueryPage(QueryPageInput input)
		{
			var module = new ListQueryPage(this, input);
			this.RunContinuableQuery(new QueryInput(module));

			return module.AsQueryPageResult();
		}

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Random">Random</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A random list of page titles.</returns>
		public IReadOnlyList<WikiTitleItem> Random(RandomInput input) => this.RunListQuery(new ListRandom(this, input));

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Recentchanges">Recentchanges</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of recent changes.</returns>
		public IReadOnlyList<RecentChangesItem> RecentChanges(RecentChangesInput input) => this.RunListQuery(new ListRecentChanges(this, input));

		/// <summary>Resets a user's password using the <see href="https://www.mediawiki.org/wiki/API:Resetpassword">Resetpassword</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the attempt to reset the password.</returns>
		public ResetPasswordResult ResetPassword(ResetPasswordInput input)
		{
			ThrowNull(input, nameof(input));
			input.Token ??= this.GetSessionToken(TokensInput.Csrf);
			return new ActionResetPassword(this).Submit(input);
		}

		/// <summary>Hides one or more revisions from those without permission to view them using the <see href="https://www.mediawiki.org/wiki/API:Revisiondelete"></see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the deleted revisions.</returns>
		public RevisionDeleteResult RevisionDelete(RevisionDeleteInput input)
		{
			ThrowNull(input, nameof(input));
			input.Token ??= this.GetSessionToken(TokensInput.Csrf);
			return new ActionRevisionDelete(this).Submit(input);
		}

		/// <summary>Rolls back all of the last user's edits to a page using the <see href="https://www.mediawiki.org/wiki/API:Rollback">Rollback</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the rollback.</returns>
		public RollbackResult Rollback(RollbackInput input)
		{
			ThrowNull(input, nameof(input));
			input.Token ??= this.CheckToken(
					input.Title == null
					? this.CheckedTokenManager.RollbackToken(input.PageId)
					: this.CheckedTokenManager.RollbackToken(input.Title));
			return new ActionRollback(this).Submit(input);
		}

		/// <summary>Returns Really Simple Discovery information using the <see href="https://www.mediawiki.org/wiki/API:Rsd">Rsd</see> API module.</summary>
		/// <returns>The raw XML of the RSD schema.</returns>
		public string Rsd() => new ActionRsd(this).Submit(NullObject.Null).Result;

		/// <summary>Searches for wiki pages that fulfil given criteria using the <see href="https://www.mediawiki.org/wiki/API:Search">Search</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of page titles fulfilling the criteria, along with information about the search hit on each page.</returns>
		/// <seealso cref="OpenSearch" />
		/// <seealso cref="PrefixSearch" />
		public SearchResult Search(SearchInput input)
		{
			var module = new ListSearch(this, input);
			this.RunContinuableQuery(new QueryInput(module));

			return module.AsSearchResult();
		}

		/// <summary>Sets the notification timestamp for watched pages, marking revisions as being read/unread using the <see href="https://www.mediawiki.org/wiki/API:Setnotificationtimestamp">Setnotificationtimestamp</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of page titles along with various informaiton about the change.</returns>
		public PageSetResult<SetNotificationTimestampItem> SetNotificationTimestamp(SetNotificationTimestampInput input)
		{
			ThrowNull(input, nameof(input));
			input.Token ??= this.GetSessionToken(TokensInput.Csrf);
			return new ActionSetNotificationTimestamp(this).SubmitPageSet(input);
		}

		/// <summary>Returns information about the site using the <see href="https://www.mediawiki.org/wiki/API:Siteinfo">Siteinfo</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>The requested site information.</returns>
		public SiteInfoResult SiteInfo(SiteInfoInput input) => this.RunModuleQuery(new MetaSiteInfo(this, input));

		/// <summary>Returns information about <see href="https://www.mediawiki.org/wiki/Manual:UploadStash">stashed</see> files using the <see href="https://www.mediawiki.org/wiki/API:Stashimageinfo">Stashimageinfo</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of image information for each image or chunk.</returns>
		public IReadOnlyList<ImageInfoItem> StashImageInfo(StashImageInfoInput input) => this.RunModuleQuery(new PropStashImageInfo(this, input)).AsReadOnlyList();

		/// <summary>Adds or removes tags based on revision IDs, log IDs, or Recent Changes IDs using the <see href="https://www.mediawiki.org/wiki/API:Tag">Tag</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the tag/untag.</returns>
		public IReadOnlyList<TagItem> Tag(TagInput input)
		{
			ThrowNull(input, nameof(input));
			input.Token ??= this.GetSessionToken(TokensInput.Csrf);
			return new ActionTag(this).Submit(input);
		}

		/// <summary>Displays information about all tags available on the wiki using the <see href="https://www.mediawiki.org/wiki/API:Tags">Tags</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of tag information.</returns>
		public IReadOnlyList<TagsItem> Tags(TagsInput input) => this.RunListQuery(new ListTags(this, input));

		/// <summary>Unblocks a user using the <see href="https://www.mediawiki.org/wiki/API:Unblock">Unblock</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the unblock operation and the user affected.</returns>
		public UnblockResult Unblock(UnblockInput input)
		{
			ThrowNull(input, nameof(input));
			input.Token ??= this.GetSessionToken(TokensInput.Csrf);
			return new ActionUnblock(this).Submit(input);
		}

		/// <summary>Undeletes a page or specific revisions thereof (by file ID or date/time) using the <see href="https://www.mediawiki.org/wiki/API:Undelete">Undelete</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the undeleted page.</returns>
		public UndeleteResult Undelete(UndeleteInput input)
		{
			ThrowNull(input, nameof(input));
			input.Token ??= this.GetSessionToken(TokensInput.Csrf);
			return new ActionUndelete(this).Submit(input);
		}

		/// <summary>Uploads a file to the wiki using the <see href="https://www.mediawiki.org/wiki/API:Upload">Upload</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the uploaded file.</returns>
		public UploadResult Upload(UploadInput input)
		{
			ThrowNull(input, nameof(input));
			input.Token ??= this.GetSessionToken(TokensInput.Csrf);
			if (
				input.ChunkSize > 0
				&& (this.SiteVersion >= 126
					|| (this.SiteVersion >= 120 && !input.RemoteFileName.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))))
			{
				return this.UploadFileChunked(input, input.ChunkSize);
			}

			var uploadInput = new UploadInputInternal(input);
			return new ActionUpload(this).Submit(uploadInput);
		}

		/// <summary>Retrieves a user's contributions using the <see href="https://www.mediawiki.org/wiki/API:Usercontribs">Usercontribs</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of the user's contributions.</returns>
		public IReadOnlyList<UserContributionsItem> UserContributions(UserContributionsInput input) => this.RunListQuery(new ListUserContribs(this, input));

		/// <summary>Returns information about the current user using the <see href="https://www.mediawiki.org/wiki/API:Userinfo">Userinfo</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the current user.</returns>
		public UserInfoResult UserInfo(UserInfoInput input) => this.RunModuleQuery(new MetaUserInfo(this, input));

		/// <summary>Adds or removes user rights (based on rights groups) using the <see href="https://www.mediawiki.org/wiki/API:Userrights">Userrights</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>The user name and ID, and the groups they were added to or removed from.</returns>
		/// <exception cref="InvalidOperationException">Thrown when the site in use is on MediaWiki 1.23 and a user ID is provided rather than a user name.</exception>
		public UserRightsResult UserRights(UserRightsInput input)
		{
			ThrowNull(input, nameof(input));
			if (input.Token == null)
			{
				var tokenUser = input.User ?? (this.SiteVersion >= 124
					? string.Empty // Name is unnecessary for 1.24+
					: throw new InvalidOperationException(EveMessages.InvalidUserRightsRequest));
				input.Token = this.CheckToken(this.CheckedTokenManager.UserRightsToken(tokenUser));
			}

			return new ActionUserRights(this).Submit(input);
		}

		/// <summary>Retrieves information about specific users on the wiki using the <see href="https://www.mediawiki.org/wiki/API:Users">Users</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the users.</returns>
		public IReadOnlyList<UsersItem> Users(UsersInput input) => this.RunListQuery(new ListUsers(this, input));

		/// <summary>Watches or unwatches pages for the current user using the <see href="https://www.mediawiki.org/wiki/API:Watch">Watch</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of page titles and whether they were watched, unwatched, or not found.</returns>
		/// <exception cref="InvalidOperationException">Thrown when the site in use is MediaWiki 1.22 or lower and a generator or list of page/revision IDs is provided.</exception>
		public PageSetResult<WatchItem> Watch(WatchInput input)
		{
			ThrowNull(input, nameof(input));
			input.Token ??= this.GetSessionToken(TokensInput.Watch);

			if (this.SiteVersion >= 123)
			{
				return new ActionWatch(this).SubmitPageSet(input);
			}

			if (input.ListType != ListType.Titles || input.GeneratorInput != null)
			{
				throw new InvalidOperationException(EveMessages.WatchNotSupported);
			}

			var list = new List<WatchItem>();
			if (input.Values != null)
			{
				foreach (var title in input.Values)
				{
					var newInput = new WatchInput(new[] { title }) { Token = input.Token, Unwatch = input.Unwatch };
					var result = new ActionWatch(this).SubmitPageSet(newInput);
					list.Add(result.First());
				}
			}

			return new PageSetResult<WatchItem>(list);
		}

		/// <summary>Retrieve detailed information about the current user's watchlist using the <see href="https://www.mediawiki.org/wiki/API:Watchlist">Watchlist</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about each page on the user's watchlist.</returns>
		public IReadOnlyList<WatchlistItem> Watchlist(WatchlistInput input) => this.RunListQuery(new ListWatchlist(this, input));

		/// <summary>Retrieve some or all of the current user's watchlist using the <see href="https://www.mediawiki.org/wiki/API:Watchlistraw">Watchlistraw</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of page titles in the user's watchlist, and whether or not they've been changed.</returns>
		public IReadOnlyList<WatchlistRawItem> WatchlistRaw(WatchlistRawInput input) => this.RunListQuery(new ListWatchlistRaw(this, input));
		#endregion

		#region Protected Virtual Methods

		/// <summary>Raises the <see cref="CaptchaChallenge" /> event.</summary>
		/// <param name="e">The <see cref="CaptchaEventArgs" /> instance containing the event data.</param>
		protected virtual void OnCaptchaChallenge(CaptchaEventArgs e) => this.CaptchaChallenge?.Invoke(this, e);

		/// <summary>Raises the <see cref="Initialized" /> event.</summary>
		/// <param name="e">The <see cref="InitializationEventArgs"/> instance containing the event data.</param>
		protected virtual void OnInitialized(InitializationEventArgs e) => this.Initialized?.Invoke(this, e);

		/// <summary>Raises the <see cref="Initializing" /> event.</summary>
		/// <param name="e">The <see cref="InitializationEventArgs"/> instance containing the event data.</param>
		protected virtual void OnInitializing(InitializationEventArgs e) => this.Initializing?.Invoke(this, e);

		/// <summary>Raises the <see cref="ResponseReceived" /> event.</summary>
		/// <param name="e">The <see cref="ResponseEventArgs" /> instance containing the event data.</param>
		protected virtual void OnResponseReceived(ResponseEventArgs e) => this.ResponseReceived?.Invoke(this, e);

		/// <summary>Raises the <see cref="SendingRequest" /> event.</summary>
		/// <param name="e">The <see cref="RequestEventArgs" /> instance containing the event data.</param>
		protected virtual void OnSendingRequest(RequestEventArgs e) => this.SendingRequest?.Invoke(this, e);

		/// <summary>Raises the <see cref="WarningOccurred" /> event.</summary>
		/// <param name="e">The <see cref="WarningEventArgs" /> instance containing the event data.</param>
		protected virtual void OnWarningOccurred(WarningEventArgs e) => this.WarningOccurred?.Invoke(this, e);
		#endregion

		#region Private Methods
		private string CheckToken(string? token) => token ?? throw new WikiException(EveMessages.InvalidToken);

		private void Clear()
		{
			// This should be kept in sync with the Initialize() method to clear anything it sets.
			this.ArticlePath = null;
			this.ContinueVersion = 0;
			this.CustomStopCheck = null;
			this.Flags = SiteInfoFlags.None;
			this.LanguageCode = null;
			this.Script = null;
			this.SiteName = null;
			this.SiteVersion = 0;
			this.SupportsMaxLag = false;
			this.TokenManager = null;
			this.UseLanguage = null;
			this.interwikiPrefixes.Clear();
			this.namespaces.Clear();
		}

		private string GetSessionToken(string type) => this.CheckToken(this.CheckedTokenManager.SessionToken(type));

		private IReadOnlyList<TOutput> RunListQuery<TInput, TOutput>(ListModule<TInput, TOutput> module)
			where TInput : class
			where TOutput : class => this.RunModuleQuery(module).AsReadOnlyList();

		private UploadResult UploadFileChunked(UploadInput input, int chunkSize)
		{
			var uploadInput = new UploadInputInternal();
			uploadInput.InitialChunk(input);
			UploadResult result;
			var readBytes = 0;
			do
			{
				uploadInput.NextChunk(input.FileData, chunkSize);
				result = new ActionUpload(this).Submit(uploadInput);
				uploadInput.Offset += chunkSize;
				uploadInput.FileKey = result.FileKey;
			}
			while (result?.Result == "Continue" && readBytes > 0);

			if (result.Result == "Success")
			{
				uploadInput.FinalChunk(input);
				uploadInput.FileKey = result.FileKey;
				result = new ActionUpload(this).Submit(uploadInput);
			}

			return result;
		}
		#endregion

		#region Private Classes
		private class BacklinksOutputComparer : EqualityComparer<BacklinksItem>
		{
			#region Public Override Methods
			public override bool Equals(BacklinksItem x, BacklinksItem y) => x?.PageId == y?.PageId;

			public override int GetHashCode(BacklinksItem obj) => obj?.PageId.GetHashCode() ?? 0;
			#endregion
		}
		#endregion
	}
}
 