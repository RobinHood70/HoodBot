namespace RobinHood70.WallE.Test
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;

	/// <inheritdoc/>
	[SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "High class coupling is the result of using classes for inputs, which is a recommended design when dealing with such a high level of input variability.")]
	public class WikiAbstractionLayer : IWikiAbstractionLayer
	{
		// Only the basics are implemented for now; the rest can come later, as needed.
		#region Fields
		private static readonly SiteInfoGeneral SiteInfoGeneral = new(
			articlePath: "/$1",
			basePage: string.Empty,
			dbType: string.Empty,
			dbVersion: string.Empty,
			externalImages: new List<string>(),
			fallback8BitEncoding: "windows-1252",
			fallbackLanguages: new List<string>(),
			favicon: null,
			flags: SiteInfoFlags.WriteApi,
			generator: "MediaWiki 1.30.0",
			gitBranch: null,
			gitHash: null,
			hhvmVersion: null,
			imageLimits: new Dictionary<string, ImageLimitsItem>(StringComparer.Ordinal)
			{
				["0"] = new ImageLimitsItem(320, 240)
			},
			language: "en",
			legalTitleChars: null,
			linkPrefix: null,
			linkPrefixCharset: null,
			linkTrail: null,
			logo: null,
			mainPage: "Main Page",
			maxUploadSize: 0,
			phpSapi: string.Empty,
			phpVersion: string.Empty,
			readOnlyReason: null,
			revision: 0,
			script: "/index.php",
			scriptPath: string.Empty,
			server: string.Empty,
			serverName: null,
			siteName: "NullWiki",
			thumbLimits: new Dictionary<string, int>(StringComparer.Ordinal)
			{
				["0"] = 120
			},
			time: DateTime.Now,
			timeOffset: TimeSpan.Zero,
			timeZone: "UTC",
			variantArticlePath: null,
			variants: new List<string>(),
			wikiId: string.Empty);

		private static readonly List<SiteInfoNamespace> SiteInfoNamespaces = new()
		{
			new SiteInfoNamespace(-2, "Media", null, NamespaceFlags.None, "Media"),
			new SiteInfoNamespace(-1, "Special", null, NamespaceFlags.None, "Special"),
			new SiteInfoNamespace(0, string.Empty, null, NamespaceFlags.ContentSpace, string.Empty),
			new SiteInfoNamespace(1, "Talk", null, NamespaceFlags.Subpages, "Talk"),
			new SiteInfoNamespace(2, "User", null, NamespaceFlags.Subpages, "User"),
			new SiteInfoNamespace(3, "User talk", null, NamespaceFlags.Subpages, "User talk"),
			new SiteInfoNamespace(4, "Project", null, NamespaceFlags.Subpages, "Project"),
			new SiteInfoNamespace(5, "Project talk", null, NamespaceFlags.Subpages, "Project talk"),
			new SiteInfoNamespace(6, "File", null, NamespaceFlags.None, "File"),
			new SiteInfoNamespace(7, "File talk", null, NamespaceFlags.Subpages, "File talk"),
			new SiteInfoNamespace(8, "MediaWiki", null, NamespaceFlags.None, "MediaWiki"),
			new SiteInfoNamespace(9, "MediaWiki talk", null, NamespaceFlags.Subpages, "MediaWiki talk"),
			new SiteInfoNamespace(10, "Template", null, NamespaceFlags.Subpages, "Template"),
			new SiteInfoNamespace(11, "Template talk", null, NamespaceFlags.Subpages, "Template talk"),
			new SiteInfoNamespace(12, "Help", null, NamespaceFlags.Subpages, "Help"),
			new SiteInfoNamespace(13, "Help talk", null, NamespaceFlags.Subpages, "Help talk"),
			new SiteInfoNamespace(14, "Category", null, NamespaceFlags.Subpages, "Category"),
			new SiteInfoNamespace(15, "Category talk", null, NamespaceFlags.Subpages, "Category talk"),
		};

		private static readonly List<SiteInfoNamespaceAlias> SiteInfoNamespaceAliases = new()
		{
			new SiteInfoNamespaceAlias(6, "Image"),
			new SiteInfoNamespaceAlias(7, "Image talk"),
		};

		private static readonly List<SiteInfoInterwikiMap> SiteInfoInterwikiMap = new()
		{
			new SiteInfoInterwikiMap("en", "file://Test.txt/$1", null, InterwikiMapFlags.Local | InterwikiMapFlags.LocalInterwiki, "English", null, null, null),
			new SiteInfoInterwikiMap("mediawikiwiki", "https://www.mediawiki.org/wiki/$1", null, InterwikiMapFlags.None, null, null, null, null),
		};
		#endregion

		#region Fields
		private readonly List<ErrorItem> warnings = new();
		#endregion

		#region Public Events

		/// <inheritdoc/>
		public event StrongEventHandler<IWikiAbstractionLayer, CaptchaEventArgs>? CaptchaChallenge;

		/// <inheritdoc/>
		public event StrongEventHandler<IWikiAbstractionLayer, EventArgs>? Initialized;

		/// <inheritdoc/>
		public event StrongEventHandler<IWikiAbstractionLayer, InitializingEventArgs>? Initializing;

		/// <inheritdoc/>
		public event StrongEventHandler<IWikiAbstractionLayer, WarningEventArgs>? WarningOccurred;
		#endregion

		#region Public Properties

		/// <inheritdoc/>
		public SiteInfoResult? AllSiteInfo { get; private set; }

		/// <inheritdoc/>
		public string? Assert { get; set; }

		/// <inheritdoc/>
		public DateTime? CurrentTimestamp => DateTime.Now.Subtract(TimeSpan.FromSeconds(1));

		/// <inheritdoc/>
		public UserInfoResult? CurrentUserInfo { get; private set; }

		/// <inheritdoc/>
		public Func<bool>? CustomStopCheck { get; set; }

		/// <inheritdoc/>
		public int SiteVersion { get; }

		/// <inheritdoc/>
		public StopCheckMethods StopCheckMethods { get; set; }

		/// <inheritdoc/>
		public int UserCheckFrequency { get; set; }

		/// <inheritdoc/>
		public StopCheckMethods ValidStopCheckMethods { get; }

		/// <inheritdoc/>
		public IReadOnlyList<ErrorItem> Warnings => this.warnings;
		#endregion

		#region Public Methods

		/// <inheritdoc/>
		public void AddWarning(string code, string info) => this.warnings.Add(new ErrorItem(code, info));

		/// <inheritdoc/>
		public IReadOnlyList<AllCategoriesItem> AllCategories(AllCategoriesInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<AllRevisionsItem> AllDeletedRevisions(AllDeletedRevisionsInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<AllLinksItem> AllFileUsages(AllFileUsagesInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<AllImagesItem> AllImages(AllImagesInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<AllLinksItem> AllLinks(AllLinksInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<AllMessagesItem> AllMessages(AllMessagesInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<WikiTitleItem> AllPages(AllPagesInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<AllLinksItem> AllRedirects(AllRedirectsInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<AllRevisionsItem> AllRevisions(AllRevisionsInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<AllLinksItem> AllTransclusions(AllTransclusionsInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<AllUsersItem> AllUsers(AllUsersInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<BacklinksItem> Backlinks(BacklinksInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public BlockResult Block(BlockInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<BlocksResult> Blocks(BlocksInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<CategoryMembersItem> CategoryMembers(CategoryMembersInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public CheckTokenResult CheckToken(CheckTokenInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public bool ClearHasMessage() => true;

		/// <inheritdoc/>
		public void ClearWarnings() => this.warnings.Clear();

		/// <inheritdoc/>
		public CompareResult Compare(CompareInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public CreateAccountResult CreateAccount(CreateAccountInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public DeleteResult Delete(DeleteInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<DeletedRevisionsItem> DeletedRevisions(ListDeletedRevisionsInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public void Download(DownloadInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public EditResult Edit(EditInput input)
		{
			input.ThrowNull(nameof(input));
			return new EditResult("Success", input.PageId, input.Title ?? "Test Page", EditFlags.NoChange, input.ContentModel, 1, 1, this.CurrentTimestamp, new Dictionary<string, string>(StringComparer.Ordinal));
		}

		/// <inheritdoc/>
		public EmailUserResult EmailUser(EmailUserInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public ExpandTemplatesResult ExpandTemplates(ExpandTemplatesInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<ExternalUrlUsageItem> ExternalUrlUsage(ExternalUrlUsageInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public string? FeedContributions(FeedContributionsInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public string? FeedRecentChanges(FeedRecentChangesInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public string? FeedWatchlist(FeedWatchlistInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<FileArchiveItem> FileArchive(FileArchiveInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<FileRepositoryInfoItem> FileRepositoryInfo(FileRepositoryInfoInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public FileRevertResult FileRevert(FileRevertInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public SiteInfoResult SiteInfo(SiteInfoInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public HelpResult Help(HelpInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public PageSetResult<ImageRotateItem> ImageRotate(ImageRotateInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<ImportItem> Import(ImportInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public void Initialize()
		{
			this.OnInitializing(new InitializingEventArgs(new SiteInfoInput(SiteInfoProperties.None)));

			var siteInfo = new SiteInfoResult(
				general: SiteInfoGeneral,
				defaultOptions: ImmutableDictionary<string, object>.Empty,
				defaultSkin: null,
				extensions: Array.Empty<SiteInfoExtensions>(),
				extensionTags: Array.Empty<string>(),
				fileExtensions: Array.Empty<string>(),
				functionHooks: Array.Empty<string>(),
				interwikiMap: SiteInfoInterwikiMap,
				lagInfo: Array.Empty<SiteInfoLag>(),
				languages: Array.Empty<SiteInfoLanguage>(),
				libraries: Array.Empty<SiteInfoLibrary>(),
				magicWords: new List<SiteInfoMagicWord>(),
				namespaces: SiteInfoNamespaces,
				namespaceAliases: SiteInfoNamespaceAliases,
				protocols: Array.Empty<string>(),
				restrictions: null,
				rights: null,
				subscribedHooks: Array.Empty<SiteInfoSubscribedHook>(),
				skins: Array.Empty<SiteInfoSkin>(),
				specialPageAliases: Array.Empty<SiteInfoSpecialPageAlias>(),
				statistics: null,
				userGroups: Array.Empty<SiteInfoUserGroup>(),
				variables: Array.Empty<string>());

			this.AllSiteInfo = siteInfo;
			this.CurrentUserInfo ??= GetUser(0, "192.0.2.1");
			this.OnInitialized();
		}

		/// <inheritdoc/>
		public IReadOnlyList<InterwikiBacklinksItem> InterwikiBacklinks(InterwikiBacklinksInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public bool IsEnabled() => true;

		/// <inheritdoc/>
		public IReadOnlyList<LanguageBacklinksItem> LanguageBacklinks(LanguageBacklinksInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public PageSetResult<PageItem> LoadPages(QueryPageSetInput pageSetInput, IEnumerable<IPropertyInput> propertyInputs) => throw new NotImplementedException();

		/// <inheritdoc/>
		public PageSetResult<PageItem> LoadPages(QueryPageSetInput pageSetInput, IEnumerable<IPropertyInput> propertyInputs, TitleCreator<PageItem> pageFactory) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<LogEventsItem> LogEvents(LogEventsInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public LoginResult Login(LoginInput input)
		{
			this.CurrentUserInfo = GetUser(1, input.NotNull(nameof(input)).UserName);
			this.Initialize();

			return LoginResult.AlreadyLoggedIn(this.CurrentUserInfo.UserId, this.CurrentUserInfo.Name);
		}

		/// <inheritdoc/>
		public void Logout() => throw new NotImplementedException();

		/// <inheritdoc/>
		public ManageTagsResult ManageTags(ManageTagsInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public MergeHistoryResult MergeHistory(MergeHistoryInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<MoveItem> Move(MoveInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<OpenSearchItem> OpenSearch(OpenSearchInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public void Options(OptionsInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<string> PagePropertyNames(PagePropertyNamesInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<PagesWithPropertyItem> PagesWithProperty(PagesWithPropertyInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyDictionary<string, ParameterInfoItem> ParameterInfo(ParameterInfoInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public ParseResult Parse(ParseInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public PatrolResult Patrol(PatrolInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<WikiTitleItem> PrefixSearch(PrefixSearchInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public ProtectResult Protect(ProtectInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<ProtectedTitlesItem> ProtectedTitles(ProtectedTitlesInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public PageSetResult<PurgeItem> Purge(PurgeInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public QueryPageResult QueryPage(QueryPageInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<WikiTitleItem> Random(RandomInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<RecentChangesItem> RecentChanges(RecentChangesInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public ResetPasswordResult ResetPassword(ResetPasswordInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public RevisionDeleteResult RevisionDelete(RevisionDeleteInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public RollbackResult Rollback(RollbackInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public string? Rsd() => throw new NotImplementedException();

		/// <inheritdoc/>
		public SearchResult Search(SearchInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public PageSetResult<SetNotificationTimestampItem> SetNotificationTimestamp(SetNotificationTimestampInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<ImageInfoItem> StashImageInfo(StashImageInfoInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<TagItem> Tag(TagInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<TagsItem> Tags(TagsInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public UnblockResult Unblock(UnblockInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public UndeleteResult Undelete(UndeleteInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public UploadResult Upload(UploadInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<UserContributionsItem> UserContributions(UserContributionsInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public UserInfoResult UserInfo(UserInfoInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public UserRightsResult UserRights(UserRightsInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<UsersItem> Users(UsersInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public PageSetResult<WatchItem> Watch(WatchInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<WatchlistItem> Watchlist(WatchlistInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<WatchlistRawItem> WatchlistRaw(WatchlistRawInput input) => throw new NotImplementedException();
		#endregion

		#region Protected Virtual Methods

		/// <summary>Raises the <see cref="CaptchaChallenge" /> event.</summary>
		/// <param name="e">The <see cref="CaptchaEventArgs" /> instance containing the event data.</param>
		protected virtual void OnCaptchaChallenge(CaptchaEventArgs e) => this.CaptchaChallenge?.Invoke(this, e);

		/// <summary>Raises the <see cref="Initialized" /> event.</summary>
		protected virtual void OnInitialized() => this.Initialized?.Invoke(this, EventArgs.Empty);

		/// <summary>Raises the <see cref="Initializing" /> event.</summary>
		/// <param name="e">The <see cref="InitializingEventArgs"/> instance containing the event data.</param>
		protected virtual void OnInitializing(InitializingEventArgs e) => this.Initializing?.Invoke(this, e);

		/// <summary>Raises the <see cref="WarningOccurred" /> event.</summary>
		/// <param name="e">The <see cref="WarningEventArgs" /> instance containing the event data.</param>
		protected virtual void OnWarningOccurred(WarningEventArgs e) => this.WarningOccurred?.Invoke(this, e);
		#endregion

		#region Private Methods
		private static UserInfoResult GetUser(long id, string name) => new(
			baseUser: new UserItem(
				userId: id,
				name: name ?? string.Empty,
				blockedBy: null,
				blockedById: 0,
				blockExpiry: null,
				blockHidden: false,
				blockId: 0,
				blockReason: null,
				blockTimestamp: null,
				editCount: 0,
				groups: null,
				implicitGroups: null,
				registration: null,
				rights: null),
			changeableGroups: null,
			email: null,
			emailAuthenticated: null,
			flags: UserInfoFlags.None,
			options: new Dictionary<string, object>(StringComparer.Ordinal),
			preferencesToken: null,
			rateLimits: new Dictionary<string, RateLimitsItem?>(StringComparer.Ordinal),
			realName: null,
			unreadText: null);
		#endregion
	}
}
