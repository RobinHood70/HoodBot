namespace RobinHood70.WallE.Eve
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;
	using System.Globalization;
	using System.Net;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Clients;
	using RobinHood70.WallE.Design;
	using RobinHood70.WallE.Eve.Modules;
	using RobinHood70.WallE.Properties;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.CommonCode.Globals;

	/// <summary>An API-based implementation of the <see cref="IWikiAbstractionLayer" /> interface.</summary>
	/// <seealso cref="IWikiAbstractionLayer" />
	[SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "High class coupling is the result of using classes for inputs, which is a recommended design when dealing with such a high level of input variability.")]
	public class WikiAbstractionLayer : IWikiAbstractionLayer, IInternetEntryPoint, IMaxLaggable, ITokenGenerator
	{
		#region Internal Constants
		internal const int LimitSmall1 = 50;
		internal const int LimitSmall2 = 500;
		internal const string ApiDisabledCode = "apidisabled";
		#endregion

		#region Private Constants
		private const SiteInfoProperties NeededSiteInfo =
			SiteInfoProperties.General |
			SiteInfoProperties.DbReplLag |
			SiteInfoProperties.Namespaces |
			SiteInfoProperties.InterwikiMap;
		#endregion

		#region Private Static Fields
		private static readonly UserInfoInput DefaultUserInformation = new UserInfoInput() { Properties = UserInfoProperties.HasMsg };
		#endregion

		#region Fields
		private readonly HashSet<string> interwikiPrefixes = new HashSet<string>(StringComparer.Create(CultureInfo.InvariantCulture, true));
		private readonly Dictionary<int, SiteInfoNamespace> namespaces = new Dictionary<int, SiteInfoNamespace>();
		private readonly List<ErrorItem> warnings = new List<ErrorItem>();
		private readonly WikiException notInitialized = new WikiException(CurrentCulture(Messages.SiteNotInitialized, nameof(Login), nameof(Initialize)));
		private ITokenManager? tokenManager;
		private int userTalkChecksIgnored;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="WikiAbstractionLayer" /> class.</summary>
		/// <param name="client">The internet client.</param>
		/// <param name="apiUri">The URI to api.php.</param>
		[SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "As above.")]
		public WikiAbstractionLayer(IMediaWikiClient client, Uri apiUri)
		{
			ThrowNull(client, nameof(client));
			ThrowNull(apiUri, nameof(apiUri));
			if (!apiUri.AbsolutePath.EndsWith("api.php", StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException(CurrentCulture(EveMessages.InvalidApi));
			}

			this.Client = client;
			this.EntryPoint = apiUri;
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
		public event StrongEventHandler<IWikiAbstractionLayer, EventArgs>? Initialized;

		/// <summary>Occurs when the wiki is about to load initialization data.</summary>
		public event StrongEventHandler<IWikiAbstractionLayer, InitializingEventArgs>? Initializing;

		/// <inheritdoc/>
		public event StrongEventHandler<IWikiAbstractionLayer, ResponseEventArgs>? ResponseReceived;

		/// <inheritdoc/>
		public event StrongEventHandler<IWikiAbstractionLayer, RequestEventArgs>? SendingRequest;

		/// <summary>Occurs when a warning is issued by the wiki.</summary>
		public event StrongEventHandler<IWikiAbstractionLayer, WarningEventArgs>? WarningOccurred;
		#endregion

		#region Public Properties

		/// <inheritdoc/>
		public SiteInfoResult? AllSiteInfo { get; private set; }

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

		/// <summary>Gets or sets the current user information.</summary>
		/// <value>The user information.</value>
		public UserInfoResult? CurrentUserInfo { get; protected set; }

		/// <summary>Gets or sets the custom stop check function.</summary>
		/// <value>A function which returns true if the bot should stop what it's doing.</value>
		public Func<bool>? CustomStopCheck { get; set; }

		/// <summary>Gets or sets any debug information returned by an action.</summary>
		/// <value>The debug information.</value>
		/// <remarks>For future expansion. Not yet implemented.
		///
		/// If debugging is enabled, any action can return debugging information along with the normal results. If a server does so, the results will be located here.</remarks>
		public DebugInfoResult? DebugInfo { get; protected internal set; }

		/// <summary>Gets or sets the detected format version.</summary>
		/// <value>The detected format version.</value>
		/// <remarks>This should not normally need to be set, but is left as settable by derived classes, should customization be needed. Assumes version 2, then falls back to 1 in the event of an error message.</remarks>
		public int DetectedFormatVersion { get; protected internal set; } = 2;

		/// <summary>Gets the interwiki prefixes.</summary>
		/// <value>A hashset of all interwiki prefixes, to allow <see cref="PageSetRedirectItem.Interwiki"/> emulation for MW 1.24 and below.</value>
		/// <remarks>For some bizarre reason, there is no read-only collection in C# that implements the Contains method, so this is left as a writable HashSet, since it's the fastest lookup.</remarks>
		public IReadOnlyCollection<string> InterwikiPrefixes => this.interwikiPrefixes;

		/// <summary>Gets or sets the maximum length of get requests for a given site. Get requests that are longer than this will be sent as POST requests instead.</summary>
		/// <value>The maximum length of get requests.</value>
		public int MaximumGetLength { get; set; } = 8100;

		/// <summary>Gets or sets the maximum size of the page set.</summary>
		/// <value>The maximum size of the page set.</value>
		/// <remarks>This should not normally need to be set, as the bot will adjust automatically as needed. However, if you know in advance that you will be logged in as a user with lower limits (typically anyone who isn't a bot or admin), then you can save some overhead by lowering this to 50, rather than the default 500.</remarks>
		public int MaximumPageSetSize { get; set; } = LimitSmall2;

		/// <inheritdoc/>
		public int MaxLag { get; set; } = 5;

		/// <summary>Gets or sets the module factory.</summary>
		/// <value>The module factory.</value>
		/// <seealso cref="IModuleFactory" />
		public IModuleFactory ModuleFactory { get; set; }

		/// <summary>Gets the namespace collection for the site.</summary>
		/// <value>The site's namespaces.</value>
		public IReadOnlyDictionary<int, SiteInfoNamespace> Namespaces => this.namespaces;

		/// <summary>Gets or sets the detected site version.</summary>
		/// <value>The MediaWiki version for the site, expressed as an integer (i.e., MW 1.23 = 123).</value>
		/// <remarks>This should not normally need to be set, but is left as settable by derived classes, should customization be needed.</remarks>
		public int SiteVersion { get; protected set; }

		/// <summary>Gets or sets the various methods to check to see if a stop has been requested.</summary>
		/// <value>The stop methods.</value>
		public StopCheckMethods StopCheckMethods { get; set; }

		/// <inheritdoc/>
		public bool SupportsMaxLag { get; protected set; } = true; // No harm in trying until we know for sure.

		/// <summary>Gets or sets the token manager.</summary>
		/// <value>The token manager.</value>
		public ITokenManager TokenManager
		{
			get => this.tokenManager ??=
				this.SiteVersion == 0 ? throw new InvalidOperationException(CurrentCulture(Messages.SiteNotInitialized, nameof(this.Initialize), nameof(this.Login))) :
				this.SiteVersion >= TokenManagerMeta.MinimumVersion ? new TokenManagerMeta(this) :
				this.SiteVersion >= TokenManagerAction.MinimumVersion ? new TokenManagerAction(this) :
				new TokenManagerOriginal(this) as ITokenManager;
			set => this.tokenManager = value ?? throw ArgumentNull(nameof(this.TokenManager));
		}

		/// <summary>Gets or sets the base URI used for all requests. This should be the full URI to api.php (e.g., <c>https://en.wikipedia.org/w/api.php</c>).</summary>
		/// <value>The URI to use as a base.</value>
		/// <remarks>This should normally be set only by the constructor and the <see cref="MakeUriSecure(bool)" /> routine, but is left as settable by derived classes, should customization be needed.</remarks>
		/// <seealso cref="WikiAbstractionLayer(IMediaWikiClient, Uri)" />
		public Uri EntryPoint { get; protected set; }

		/// <summary>Gets or sets the language to use for responses from the wiki.</summary>
		/// <value>The use language.</value>
		public string? UseLanguage { get; set; }

		/// <summary>Gets or sets how often the user talk page should be checked for non-queries.</summary>
		/// <value>The frequency to check user name and talk page. A value of 1 or less will check with every non-query request; higher values will only check every n times.</value>
		public int UserCheckFrequency { get; set; }

		/// <summary>Gets or sets a value indicating whether to use UTF-8 encoding for responses.</summary>
		/// <value><see langword="true" /> to use UTF-8; otherwise, <see langword="false" />. Defaults to <see langword="true" />.</value>
		public bool Utf8 { get; set; } = true;

		/// <summary>Gets the stop check methods that are valid for current state.</summary>
		/// <value>The stop methods.</value>
		public StopCheckMethods ValidStopCheckMethods => (this.CurrentUserInfo == null || this.CurrentUserInfo.Flags.HasFlag(UserInfoFlags.Anonymous))
			? this.StopCheckMethods & StopCheckMethods.LoggedOut
			: this.StopCheckMethods;

		/// <summary>Gets a list of all warnings.</summary>
		/// <value>The warnings.</value>
		public IReadOnlyList<ErrorItem> Warnings => this.warnings;
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
		public Uri GetFullArticlePath(string pageName)
		{
			ThrowNull(pageName, nameof(pageName));
			var articlePath = this.ArticlePath ?? throw this.notInitialized;
			return new Uri(articlePath.Replace("$1", pageName.Replace(' ', '_'), StringComparison.Ordinal));
		}

		/// <summary>Makes the URI secure.</summary>
		/// <param name="https">If set to <see langword="true" />, forces the URI to be a secure URI (https://); if false, forces it to be insecure (http://).</param>
		public void MakeUriSecure(bool https)
		{
			var urib = new UriBuilder(this.EntryPoint)
			{
				Scheme = https ? "https" : "http",
			};
			this.EntryPoint = urib.Uri;
		}

		/// <summary>Runs the continuable query specified by the input.</summary>
		/// <param name="input">The input.</param>
		/// <remarks>This function is used internally, but also made available externally for special situations. The caller is responsible for deciding whether any given query is continuable.</remarks>
		public void RunQuery(IEnumerable<IQueryModule> input)
		{
			var query = new ActionQuery(this, input);
			query.Submit();
			this.DoStopCheck(query.UserInfo);
		}

		/// <summary>Runs the continuable query specified by the input.</summary>
		/// <param name="input">The input.</param>
		/// <remarks>This function is used internally, but also made available externally for special situations. The caller is responsible for deciding whether any given query is continuable.</remarks>
		public void RunQuery(params IQueryModule[] input) => this.RunQuery(input as IEnumerable<IQueryModule>);

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
			this.RunQuery(module);

			return module.Output ?? throw WikiException.General("null-result", module.Name + " was found in the results, but the deserializer returned null.");
		}

		/// <summary>Runs the pageset query specified by the input.</summary>
		/// <param name="input">The input.</param>
		/// <param name="pageFactory">The factory method to use to generate PageItem derivatives.</param>
		/// <remarks>This function is used internally, but also made available externally for special situations.</remarks>
		/// <returns>A list of <see cref="PageItem"/>s of the specified underlying type.</returns>
		public PageSetResult<PageItem> RunPageSetQuery(QueryInput input, TitleCreator<PageItem> pageFactory)
		{
			ThrowNull(input, nameof(input));
			ThrowNull(pageFactory, nameof(pageFactory));
			var query = new ActionQueryPageSet(this, input, pageFactory);
			var retval = query.Submit();
			this.DoStopCheck(query.UserInfo);

			return retval;
		}

		/// <summary>Converts the given request into an HTML request and submits it to the site.</summary>
		/// <param name="request">The request.</param>
		/// <returns>The site's response to the request.</returns>
		public string SendRequest(Request request)
		{
			ThrowNull(request, nameof(request));
			this.OnSendingRequest(new RequestEventArgs(request));
			string response;
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

			this.OnResponseReceived(new ResponseEventArgs(response));
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
			var eventArgs = new InitializingEventArgs(new SiteInfoInput(NeededSiteInfo));
			this.OnInitializing(eventArgs);

			// Create input from return values in eventArgs
			var siteInfoInput = new SiteInfoInput(eventArgs.Properties)
			{
				FilterLocalInterwiki = eventArgs.FilterLocalInterwiki,
				InterwikiLanguageCode = eventArgs.InterwikiLanguageCode,
				ShowAllDatabases = eventArgs.ShowAllDatabases,
				ShowNumberInGroup = eventArgs.ShowNumberInGroup,
			};

			var infoModule = new MetaSiteInfo(this, siteInfoInput);
			var userModule = new MetaUserInfo(this, DefaultUserInformation);
			this.RunQuery(infoModule, userModule);

			if (!(userModule.Output is UserInfoResult userInfo) || !(infoModule.Output is SiteInfoResult siteInfo) || siteInfo.General == null || siteInfo.Namespaces == null)
			{
				throw new WikiException(EveMessages.InitializationFailed);
			}

			this.AllSiteInfo = siteInfo;
			this.CurrentUserInfo = userInfo;

			// General
			var general = siteInfo.General;
			var path = general.ArticlePath;
			if (path.StartsWith('/'))
			{
				var repl = path.Substring(0, path.IndexOf("$1", StringComparison.Ordinal));
				var articleBaseIndex = general.BasePage.IndexOf(repl, StringComparison.Ordinal);
				if (articleBaseIndex < 0)
				{
					articleBaseIndex = general.BasePage.IndexOf("/", general.BasePage.IndexOf("//", StringComparison.Ordinal) + 2, StringComparison.Ordinal);
				}

				path = general.BasePage.Substring(0, articleBaseIndex) + path;
			}

			this.ArticlePath = path;
			var versionFudged = Regex.Replace(general.Generator, @"[^0-9\.]", ".", RegexOptions.None, DefaultRegexTimeout).TrimStart(TextArrays.Period);
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
			if (siteInfo.InterwikiMap != null)
			{
				// Should never actually be null, but not critical if it is.
				foreach (var interwiki in siteInfo.InterwikiMap)
				{
					this.interwikiPrefixes.Add(interwiki.Prefix);
				}
			}

			// DbReplLag
			this.SupportsMaxLag = siteInfo.LagInfo?.Count > 0 && siteInfo.LagInfo[0].Lag != -1;

			// Other (not SiteInfo-related)
			this.tokenManager?.Clear();
			if (this.ContinueVersion == 0)
			{
				this.ContinueVersion = siteVersion >= ContinueModule2.ContinueMinimumVersion ? 2 : 1;
			}

			this.OnInitialized();
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

			try
			{
				new ActionQuery(this, Array.Empty<IQueryModule>()).Submit();

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
				if (!string.Equals(e.Code, ApiDisabledCode, StringComparison.Ordinal))
				{
					throw;
				}
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

			this.RunQuery(modules);
			var output = new HashSet<BacklinksItem>(new BacklinksOutputComparer());
			foreach (var module in modules)
			{
				if (module.Output != null)
				{
					output.UnionWith(module.Output);
				}
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
			return this.SubmitValueAction(new ActionBlock(this), input);
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
		public CheckTokenResult CheckToken(CheckTokenInput input) => this.SubmitValueAction(new ActionCheckToken(this), input);

		/// <summary>Clears the user's "has message" flag using the <see href="https://www.mediawiki.org/wiki/API:Clearhasmsg">Clearhasmsg</see> API module or by visiting the user's talk page on wikis below version 1.24.</summary>
		/// <returns>Whether the attempt was successful.</returns>
		public bool ClearHasMessage()
		{
			try
			{
				// We don't go through the standard SubmitValueAction here, since that would perform an inappropriate stop check.
				var action = new ActionClearHasMsg(this).Submit(NullObject.Null);
				return string.Equals(action.Result, "success", StringComparison.OrdinalIgnoreCase);
			}
			catch (NotSupportedException)
			{
				if (this.CurrentUserInfo == null)
				{
					return false;
				}

				var index = this.GetFullArticlePath(this.Namespaces[MediaWikiNamespaces.UserTalk].Name + ":" + this.CurrentUserInfo.Name);
				return this.Client.Get(index).Length > 0;
			}
		}

		/// <summary>Compares two revisions or pages using the <see href="https://www.mediawiki.org/wiki/API:Compare">Compare</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the comparison.</returns>
		public CompareResult Compare(CompareInput input) => this.SubmitValueAction(new ActionCompare(this), input);

		/// <summary>Creates an account using the <see href="https://www.mediawiki.org/wiki/API:Createaccount">Createaccount</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the account created.</returns>
		public CreateAccountResult CreateAccount(CreateAccountInput input)
		{
			ThrowNull(input, nameof(input));
			var create = new ActionCreateAccount(this);
			var retval = create.Submit(input);
			if (input.Token == null)
			{
				input.Token = retval.Token;
				retval = create.Submit(input);
			}

			if (retval.CaptchaData.Count > 0)
			{
				var eventArgs = new CaptchaEventArgs(retval.CaptchaData, input.CaptchaSolution);
				this.OnCaptchaChallenge(eventArgs);
				if (eventArgs.CaptchaSolution.Count > 0)
				{
					retval = create.Submit(input);
				}
			}

			this.DoStopCheck();

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
			return this.SubmitValueAction(new ActionDelete(this), input);
		}

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Deletedrevisions">Deletedrevisions</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of deleted revisions.</returns>
		public IReadOnlyList<DeletedRevisionsItem> DeletedRevisions(ListDeletedRevisionsInput input) => this.RunListQuery(new ListDeletedRevs(this, input));

		/// <summary>Downloads the specified resource (typically, a Uri) to a file.</summary>
		/// <param name="input">The input parameters.</param>
		/// <remarks>This is not part of the API, but since Upload is, it makes sense to provide its counterpart so the end-user is not left accessing Client directly. No stop checks are performed when using this method, since this could be downloading from anywhere.</remarks>
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
			var retval = edit.Submit(input);
			if (retval.CaptchaData.Count > 0)
			{
				var eventArgs = new CaptchaEventArgs(retval.CaptchaData, input.CaptchaSolution);
				this.OnCaptchaChallenge(eventArgs);
				if (eventArgs.CaptchaSolution.Count > 0)
				{
					retval = edit.Submit(input);
				}
			}

			this.DoStopCheck();
			return retval;
		}

		/// <summary>Emails a user using the <see href="https://www.mediawiki.org/wiki/API:Emailuser">Emailuser</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the e-mail that was sent.</returns>
		public EmailUserResult EmailUser(EmailUserInput input)
		{
			ThrowNull(input, nameof(input));
			input.Token ??= this.GetSessionToken(TokensInput.Csrf);
			return this.SubmitValueAction(new ActionEmailUser(this), input);
		}

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Expandtemplates">Expandtemplates</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the expanded templates.</returns>
		public ExpandTemplatesResult ExpandTemplates(ExpandTemplatesInput input) => this.SubmitValueAction(new ActionExpandTemplates(this), input);

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Externalurlusage">Externalurlusage</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of external URLs and the pages they're used on.</returns>
		public IReadOnlyList<ExternalUrlUsageItem> ExternalUrlUsage(ExternalUrlUsageInput input) => this.RunListQuery(new ListExtUrlUsage(this, input));

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Feedcontributions">Feedcontributions</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>The raw XML of the Contributions RSS feed.</returns>
		public string? FeedContributions(FeedContributionsInput input) => this.SubmitValueAction(new ActionFeedContributions(this), input).Result;

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Feedrecentchanges">Feedrecentchanges</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>The raw XML of the Recent Changes RSS feed.</returns>
		public string? FeedRecentChanges(FeedRecentChangesInput input) => this.SubmitValueAction(new ActionFeedRecentChanges(this), input).Result;

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Feedwatchlist">Feedwatchlist</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>The raw XML of the Watchlist RSS feed.</returns>
		public string? FeedWatchlist(FeedWatchlistInput input) => this.SubmitValueAction(new ActionFeedWatchlist(this), input).Result;

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
			return this.SubmitValueAction(new ActionFileRevert(this), input);
		}

		/// <summary>Gets help information using the <see href="https://www.mediawiki.org/wiki/API:Help">Help</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>The API help for the module(s) requested.</returns>
		public HelpResult Help(HelpInput input) => this.SubmitValueAction(new ActionHelp(this), input);

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Imagerotate">Imagerotate</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of page titles with image rotation information.</returns>
		public PageSetResult<ImageRotateItem> ImageRotate(ImageRotateInput input)
		{
			ThrowNull(input, nameof(input));
			input.Token ??= this.GetSessionToken(TokensInput.Csrf);
			return this.SubmitPageSet(new ActionImageRotate(this), input);
		}

		/// <summary>Imports pages into a wiki. Correspondes to the <see href="https://www.mediawiki.org/wiki/API:Import">Import</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of page titles with import information.</returns>
		public IReadOnlyList<ImportItem> Import(ImportInput input)
		{
			ThrowNull(input, nameof(input));
			input.Token ??= this.GetSessionToken(TokensInput.Csrf);
			return this.SubmitValueAction(new ActionImport(this), input);
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
		public PageSetResult<PageItem> LoadPages(QueryPageSetInput pageSetInput, IEnumerable<IPropertyInput> propertyInputs) => this.LoadPages(pageSetInput, propertyInputs, DefaultPageFactory);

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
		/// <remarks>No stop checking is performed when logging in.</remarks>
		public LoginResult Login(LoginInput input)
		{
			ThrowNull(input, nameof(input));
			if (this.SiteVersion == 0)
			{
				this.Initialize();
			}

			if (string.IsNullOrEmpty(input.UserName))
			{
				this.tokenManager?.Clear();
				return LoginResult.EditingAnonymously(this.CurrentUserInfo?.Name);
			}

			var userNameSplit = input.UserName.Split(TextArrays.At);
			var botPasswordName = userNameSplit[^1];

			// Both checks are necessary because user names can legitimately contain @ signs.
			if (this.CurrentUserInfo is UserInfoResult userInfo && (
				string.Equals(userInfo.Name, input.UserName, StringComparison.Ordinal) ||
				string.Equals(userInfo.Name, botPasswordName, StringComparison.Ordinal)))
			{
				return LoginResult.AlreadyLoggedIn(userInfo.UserId, userInfo.Name);
			}

			// Second logins are not allowed without first logging out in later versions of the API.
			this.Logout(false); // Explicitly call non-clearing logout or the rest of the method will fail.

			input.Token ??= this.TokenManager.LoginToken();
			var assert = this.Assert;
			this.Assert = null;
			var retries = 4; // Allow up to four retries in case of throttling, plus the NeedToken portion of the request.
			LoginResult output;
			do
			{
				output = new ActionLogin(this).Submit(input); // Do NOT change this to use the normal StopCheck routines, as there shouldn't be any stop checks when logging in.
				switch (output.Result)
				{
					case "NeedToken":
						// Counts as a retry in case we're stuck in a NeedToken loop, which can happen if cookies are not handled properly or in some other rare circumstances.
						input.Token = output.Token;
						retries--;
						break;
					case "Success":
						this.CurrentUserInfo = this.UserInfo(DefaultUserInformation);
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
		/// <remarks>This call does not clear site data, since the user will most likely stop using the Site object after logging out anyway. This could also be a logout due to detecting that the wrong user is currently logged in, in which case most of the site information is still valid, and the user may even be intentionally access the site anonymously. Since the user's intent is unknown, it is safer not to assume it.
		///
		/// No stop checking is performed on logging out.</remarks>
		public void Logout() => this.Logout(false);

		/// <summary>Logs the user out using the <see href="https://www.mediawiki.org/wiki/API:Logout">Logout</see> API module.</summary>
		/// <param name="clear">Whether to clear all site data or not. If you intend to keep using the site even after logging out, set this to <see langword="false"/>; use <see langword="true"/> to clear all site-specific data, such as namespaces and site name.</param>
		/// <remarks>No stop checking is performed on logging out.</remarks>
		public void Logout(bool clear)
		{
			if (this.CurrentUserInfo is UserInfoResult userInfo && userInfo.Flags.HasFlag(UserInfoFlags.Anonymous))
			{
				var input = new LogoutInput();
				if (this.SiteVersion >= 134)
				{
					input.Token = this.GetSessionToken(TokensInput.Csrf);
				}

				var assert = this.Assert;
				this.Assert = null;
				new ActionLogout(this).Submit(input);  // Do NOT change this to use the normal StopCheck routines, as there shouldn't be any stop checks when logging out.
				this.Assert = assert;
				this.Client.SaveCookies();

				// Re-retrieve user info since the site could conceivably still be used anonymously.
				this.CurrentUserInfo = this.UserInfo(DefaultUserInformation);
			}

			if (clear)
			{
				this.Clear();
			}
			else
			{
				this.tokenManager?.Clear();
			}
		}

		/// <summary>Adds, removes, activates, or deactivates a page tag using the <see href="https://www.mediawiki.org/wiki/API:Managetags">Managetags</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the altered tag.</returns>
		public ManageTagsResult ManageTags(ManageTagsInput input)
		{
			ThrowNull(input, nameof(input));
			input.Token ??= this.GetSessionToken(TokensInput.Csrf);
			return this.SubmitValueAction(new ActionManageTags(this), input);
		}

		/// <summary>Merges the history of two pages using the <see href="https://www.mediawiki.org/wiki/API:Mergehistory">Mergehistory</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the merge.</returns>
		public MergeHistoryResult MergeHistory(MergeHistoryInput input)
		{
			ThrowNull(input, nameof(input));
			input.Token ??= this.GetSessionToken(TokensInput.Csrf);
			return this.SubmitValueAction(new ActionMergeHistory(this), input);
		}

		/// <summary>Moves a page, and optionally, it's talk/sub-pages using the <see href="https://www.mediawiki.org/wiki/API:Move">Move</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of results for each page, subpage, or talk page moved, including errors that may indicate only partial success.</returns>
		/// <remarks>Due to the fact that this method can generate multiple errors, any errors returned here will not be raised as exceptions. Results should instead be scanned for errors, and acted upon accordingly.</remarks>
		public IReadOnlyList<MoveItem> Move(MoveInput input)
		{
			ThrowNull(input, nameof(input));
			input.Token ??= this.GetSessionToken(TokensInput.Csrf);
			return this.SubmitValueAction(new ActionMove(this), input);
		}

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Opensearch">Opensearch</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of open search results.</returns>
		/// <seealso cref="PrefixSearch" />
		/// <seealso cref="Search" />
		public IReadOnlyList<OpenSearchItem> OpenSearch(OpenSearchInput input)
		{
			ThrowNull(input, nameof(input));
			return this.SubmitValueAction(new ActionOpenSearch(this), input);
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
				var singleItems = new Dictionary<string, string?>(StringComparer.Ordinal);
				string? lastKey = null;
				foreach (var changeItem in input.Change)
				{
					if (this.SiteVersion < 128 && (changeItem.Key.Contains('|', StringComparison.Ordinal) || (changeItem.Value?.Contains('|', StringComparison.Ordinal) ?? false)))
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
						this.SubmitValueAction(new ActionOptions(this), singleInput);
					}
				}
			}

			this.SubmitValueAction(new ActionOptions(this), internalInput);
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
		public IReadOnlyDictionary<string, ParameterInfoItem> ParameterInfo(ParameterInfoInput input) => this.SubmitValueAction(new ActionParamInfo(this), input);

		/// <summary>Parses custom text, a page, or a revision using the <see href="https://www.mediawiki.org/wiki/API:Parse">Parse</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>The result of the parse.</returns>
		public ParseResult Parse(ParseInput input) => this.SubmitValueAction(new ActionParse(this), input);

		/// <summary>Patrols a recent change or revision using the <see href="https://www.mediawiki.org/wiki/API:Patrol">Patrol</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>The patrolled page information along with the Recent Changes ID.</returns>
		public PatrolResult Patrol(PatrolInput input)
		{
			ThrowNull(input, nameof(input));
			input.Token ??= this.GetSessionToken(TokensInput.Patrol);
			return this.SubmitValueAction(new ActionPatrol(this), input);
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
			return this.SubmitValueAction(new ActionProtect(this), input);
		}

		/// <summary>Retrieves page titles that are creation-protected using the <see href="https://www.mediawiki.org/wiki/API:Protectedtitles">Protectedtitles</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of creation-protected articles.</returns>
		public IReadOnlyList<ProtectedTitlesItem> ProtectedTitles(ProtectedTitlesInput input) => this.RunListQuery(new ListProtectedTitles(this, input));

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Purge">Purge</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of pages that were purged, along with information about the purge for each page.</returns>
		public PageSetResult<PurgeItem> Purge(PurgeInput input) => this.SubmitPageSet(new ActionPurge(this), input);

		/// <summary>Returns data from the <see href="https://www.mediawiki.org/wiki/API:Querypage">Querypage</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of pages titles and, when available, the related value. Other fields will be returned as a set of name-value pairs in the <see cref="QueryPageItem.DatabaseResult" /> dictionary.</returns>
		public QueryPageResult QueryPage(QueryPageInput input)
		{
			var module = new ListQueryPage(this, input);
			this.RunQuery(module);

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
			return this.SubmitValueAction(new ActionResetPassword(this), input);
		}

		/// <summary>Hides one or more revisions from those without permission to view them using the <see href="https://www.mediawiki.org/wiki/API:Revisiondelete"></see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the deleted revisions.</returns>
		public RevisionDeleteResult RevisionDelete(RevisionDeleteInput input)
		{
			ThrowNull(input, nameof(input));
			input.Token ??= this.GetSessionToken(TokensInput.Csrf);
			return this.SubmitValueAction(new ActionRevisionDelete(this), input);
		}

		/// <summary>Rolls back all of the last user's edits to a page using the <see href="https://www.mediawiki.org/wiki/API:Rollback">Rollback</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the rollback.</returns>
		public RollbackResult Rollback(RollbackInput input)
		{
			ThrowNull(input, nameof(input));
			input.Token ??= CheckToken(
					input.Title == null
					? this.TokenManager.RollbackToken(input.PageId)
					: this.TokenManager.RollbackToken(input.Title),
					TokensInput.Rollback);
			return this.SubmitValueAction(new ActionRollback(this), input);
		}

		/// <summary>Returns Really Simple Discovery information using the <see href="https://www.mediawiki.org/wiki/API:Rsd">Rsd</see> API module.</summary>
		/// <returns>The raw XML of the RSD schema.</returns>
		/// <remarks>No stop checking is performed when using this method.</remarks>
		public string? Rsd() => new ActionRsd(this).Submit(NullObject.Null).Result;

		/// <summary>Searches for wiki pages that fulfil given criteria using the <see href="https://www.mediawiki.org/wiki/API:Search">Search</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of page titles fulfilling the criteria, along with information about the search hit on each page.</returns>
		/// <seealso cref="OpenSearch" />
		/// <seealso cref="PrefixSearch" />
		public SearchResult Search(SearchInput input)
		{
			var module = new ListSearch(this, input);
			this.RunQuery(module);

			return module.AsSearchResult();
		}

		/// <summary>Sets the notification timestamp for watched pages, marking revisions as being read/unread using the <see href="https://www.mediawiki.org/wiki/API:Setnotificationtimestamp">Setnotificationtimestamp</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of page titles along with various informaiton about the change.</returns>
		public PageSetResult<SetNotificationTimestampItem> SetNotificationTimestamp(SetNotificationTimestampInput input)
		{
			ThrowNull(input, nameof(input));
			input.Token ??= this.GetSessionToken(TokensInput.Csrf);
			return this.SubmitPageSet(new ActionSetNotificationTimestamp(this), input);
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
			return this.SubmitValueAction(new ActionTag(this), input);
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
			return this.SubmitValueAction(new ActionUnblock(this), input);
		}

		/// <summary>Undeletes a page or specific revisions thereof (by file ID or date/time) using the <see href="https://www.mediawiki.org/wiki/API:Undelete">Undelete</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the undeleted page.</returns>
		public UndeleteResult Undelete(UndeleteInput input)
		{
			ThrowNull(input, nameof(input));
			input.Token ??= this.GetSessionToken(TokensInput.Csrf);
			return this.SubmitValueAction(new ActionUndelete(this), input);
		}

		/// <summary>Uploads a file to the wiki using the <see href="https://www.mediawiki.org/wiki/API:Upload">Upload</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the uploaded file.</returns>
		/// <remarks>Unlike the <see cref="Download(DownloadInput)"/> method, this method performs stop checking after every upload, since it can only upload to the current wiki.</remarks>
		public UploadResult Upload(UploadInput input)
		{
			ThrowNull(input, nameof(input));
			input.Token ??= this.GetSessionToken(TokensInput.Csrf);
			if (input.ChunkSize > 0
				&& (this.SiteVersion >= 126
				|| (this.SiteVersion >= 120 && !input.RemoteFileName.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))))
			{
				return this.UploadFileChunked(input);
			}

			var uploadInput = new UploadInputInternal(input);
			return this.SubmitValueAction(new ActionUpload(this), uploadInput);
		}

		/// <summary>Retrieves a user's contributions using the <see href="https://www.mediawiki.org/wiki/API:Usercontribs">Usercontribs</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of the user's contributions.</returns>
		public IReadOnlyList<UserContributionsItem> UserContributions(UserContributionsInput input) => this.RunListQuery(new ListUserContribs(this, input));

		/// <summary>Returns information about the current user using the <see href="https://www.mediawiki.org/wiki/API:Userinfo">Userinfo</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the current user. Note that due to WallE's stop-checking, you may get additional information you didn't request—specifically: block information and the HasMessage flag.</returns>
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
				input.Token = CheckToken(this.TokenManager.UserRightsToken(tokenUser), TokensInput.UserRights);
			}

			return this.SubmitValueAction(new ActionUserRights(this), input);
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
				return this.SubmitPageSet(new ActionWatch(this), input);
			}

			if (input.ListType != ListType.Titles || input.GeneratorInput != null)
			{
				throw new InvalidOperationException(EveMessages.WatchNotSupported);
			}

			// Watch each page individually, then merge the individual result into a constructed PageSetResult.
			var list = new List<WatchItem>();
			if (input.Values != null)
			{
				foreach (var title in input.Values)
				{
					var newInput = new WatchInput(new[] { title }) { Token = input.Token, Unwatch = input.Unwatch };
					var result = this.SubmitPageSet(new ActionWatch(this), newInput);
					foreach (var item in result)
					{
						list.Add(item);
					}
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

		#region Protected Methods

		/// <summary>Performs any stop checks flagged in <see cref="ValidStopCheckMethods"/> (derived from <see cref="StopCheckMethods"/>).</summary>
		/// <remarks>This version is used for actions other than queries.</remarks>
		protected void DoStopCheck() => this.DoStopCheck(null);
		#endregion

		#region Protected Virtual Methods

		/// <summary>Performs any stop checks flagged in <see cref="ValidStopCheckMethods"/> (derived from <see cref="StopCheckMethods"/>).</summary>
		/// <param name="userInfoResult">The user information returned by the query. Set to <see langword="null"/> if not a query or integrated check is disabled.</param>
		/// <remarks>This version uses the integrated query results, if available.</remarks>
		protected virtual void DoStopCheck(UserInfoResult? userInfoResult)
		{
			if (this.ValidStopCheckMethods.HasFlag(StopCheckMethods.Custom) && (this.CustomStopCheck?.Invoke() == true))
			{
				throw new StopException(EveMessages.CustomStopCheckFailed);
			}

			if ((this.ValidStopCheckMethods & (StopCheckMethods.TalkChecks | StopCheckMethods.UserNameCheck)) != StopCheckMethods.None)
			{
				if (userInfoResult == null)
				{
					Debug.Assert(this.ValidStopCheckMethods.HasFlag(StopCheckMethods.TalkCheckQuery), "Something's not right here, this should've been an integrated check!");
					if (this.userTalkChecksIgnored >= this.UserCheckFrequency)
					{
						var input = DefaultUserInformation;
						input.Properties |= UserInfoProperties.HasMsg; // Default should cover this, but if that's ever changed, this still *must* have a HasMsg check.
						userInfoResult = this.UserInfo(input);
						if (userInfoResult == null)
						{
							throw WikiException.General("userinfo-failed", EveMessages.UserInfoCheckFailed);
						}

						this.userTalkChecksIgnored = 0;
					}
					else
					{
						this.userTalkChecksIgnored++;
					}
				}

				if (userInfoResult != null)
				{
					if (this.ValidStopCheckMethods.HasFlag(StopCheckMethods.UserNameCheck)
						&& this.SiteVersion < 128
						&& !string.Equals(this.CurrentUserInfo?.Name, userInfoResult.Name, StringComparison.Ordinal))
					{
						// Used to check if username has unexpectedly changed, indicating that the bot has been logged out (or conceivably logged in) unexpectedly.
						throw new StopException(EveMessages.UserNameChanged);
					}

					if (userInfoResult.Flags.HasFlag(UserInfoFlags.HasMessage)
						&& ((this.ValidStopCheckMethods & StopCheckMethods.TalkChecks) != StopCheckMethods.None))
					{
						throw new StopException(EveMessages.TalkPageChanged);
					}
				}
			}
		}

		/// <summary>Raises the <see cref="CaptchaChallenge" /> event.</summary>
		/// <param name="e">The <see cref="CaptchaEventArgs" /> instance containing the event data.</param>
		protected virtual void OnCaptchaChallenge(CaptchaEventArgs e) => this.CaptchaChallenge?.Invoke(this, e);

		/// <summary>Raises the <see cref="Initialized" /> event.</summary>
		protected virtual void OnInitialized() => this.Initialized?.Invoke(this, EventArgs.Empty);

		/// <summary>Raises the <see cref="Initializing" /> event.</summary>
		/// <param name="e">The <see cref="InitializingEventArgs"/> instance containing the event data.</param>
		protected virtual void OnInitializing(InitializingEventArgs e) => this.Initializing?.Invoke(this, e);

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
		private static string CheckToken(string? token, string type) => token ?? throw new WikiException(CurrentCulture(EveMessages.InvalidToken, type));

		private void Clear()
		{
			// This should be kept in sync with the Initialize() method to clear anything it sets.
			this.AllSiteInfo = null;
			this.ArticlePath = null;
			this.ContinueVersion = 0;
			this.CustomStopCheck = null;
			this.SiteVersion = 0;
			this.SupportsMaxLag = false;
			this.UseLanguage = null;
			this.interwikiPrefixes.Clear();
			this.namespaces.Clear();
			this.tokenManager?.Clear(); // Deliberately clearing underlying property so we're not initializing it only to clear it.
		}

		private string GetSessionToken(string type) => CheckToken(this.TokenManager.SessionToken(type), type);

		private IReadOnlyList<TOutput> RunListQuery<TInput, TOutput>(ListModule<TInput, TOutput> module)
			where TInput : class
			where TOutput : class => this.RunModuleQuery(module).AsReadOnlyList();

		private PageSetResult<TOutput> SubmitPageSet<TInput, TOutput>(ActionModulePageSet<TInput, TOutput> action, TInput input)
			where TInput : PageSetInput
			where TOutput : ITitle
		{
			var retval = action.Submit(input);
			this.DoStopCheck();
			return retval;
		}

		private TOutput SubmitValueAction<TInput, TOutput>(ActionModule<TInput, TOutput> action, TInput input)
			where TInput : class
			where TOutput : class
		{
			var retval = action.Submit(input);
			this.DoStopCheck();
			return retval;
		}

		private UploadResult UploadFileChunked(UploadInput input)
		{
			var uploadInput = new UploadInputInternal(input);
			UploadResult result;
			var readBytes = 0;
			do
			{
				uploadInput.NextChunk(input.FileData, input.ChunkSize);
				result = this.SubmitValueAction(new ActionUpload(this), uploadInput);
				uploadInput.Offset += input.ChunkSize;
				uploadInput.FileKey = result.FileKey;
			}
			while (string.Equals(result.Result, "Continue", StringComparison.OrdinalIgnoreCase) && readBytes > 0);

			if (string.Equals(result.Result, "Success", StringComparison.OrdinalIgnoreCase))
			{
				uploadInput.FinalChunk(input);
				uploadInput.FileKey = result.FileKey;
				result = this.SubmitValueAction(new ActionUpload(this), uploadInput);
			}

			return result;
		}
		#endregion

		#region private sealed classes
		private sealed class BacklinksOutputComparer : EqualityComparer<BacklinksItem>
		{
			#region Public Override Methods
			public override bool Equals(BacklinksItem? x, BacklinksItem? y) => x?.PageId == y?.PageId;

			public override int GetHashCode(BacklinksItem obj) => obj?.PageId.GetHashCode() ?? 0;
			#endregion
		}
		#endregion
	}
}
