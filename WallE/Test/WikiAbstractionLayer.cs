namespace RobinHood70.WallE.Test
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using static RobinHood70.CommonCode.Globals;

	/// <inheritdoc/>
	public class WikiAbstractionLayer : IWikiAbstractionLayer
	{
		// Only the basics are implemented for now; the rest can come later, as needed.
		#region Public Events

		/// <inheritdoc/>
		public event StrongEventHandler<IWikiAbstractionLayer, CaptchaEventArgs>? CaptchaChallenge;

		/// <inheritdoc/>
		public event StrongEventHandler<IWikiAbstractionLayer, InitializedEventArgs>? Initialized;

		/// <inheritdoc/>
		public event StrongEventHandler<IWikiAbstractionLayer, InitializingEventArgs>? Initializing;

		/// <inheritdoc/>
		public event StrongEventHandler<IWikiAbstractionLayer, WarningEventArgs>? WarningOccurred;
		#endregion

		#region Public Properties

		/// <inheritdoc/>
		public string? Assert { get; set; }

		/// <inheritdoc/>
		public DateTime? CurrentTimestamp => DateTime.Now.Subtract(TimeSpan.FromSeconds(1));

		/// <inheritdoc/>
		public UserInfoResult? CurrentUserInfo { get; private set; }

		/// <inheritdoc/>
		public Func<bool>? CustomStopCheck { get; set; }

		/// <inheritdoc/>
		public SiteInfoFlags Flags => SiteInfoFlags.WriteApi;

		/// <inheritdoc/>
		public string? LanguageCode => "en";

		/// <inheritdoc/>
		public string? SiteName => "Test";

		/// <inheritdoc/>
		public int SiteVersion => 130;

		/// <inheritdoc/>
		public StopCheckMethods StopCheckMethods { get; set; }

		/// <inheritdoc/>
		public int UserCheckFrequency { get; set; }

		/// <inheritdoc/>
		public StopCheckMethods ValidStopCheckMethods { get; }

		/// <inheritdoc/>
		public IReadOnlyList<ErrorItem> Warnings { get; } = new List<ErrorItem>();
		#endregion

		#region Public Methods

		/// <inheritdoc/>
		public void AddWarning(string code, string info)
		{
			return;
		}

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
		public void ClearWarnings()
		{
			return;
		}

		/// <inheritdoc/>
		public CompareResult Compare(CompareInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public CreateAccountResult CreateAccount(CreateAccountInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public DeleteResult Delete(DeleteInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<DeletedRevisionsItem> DeletedRevisions(ListDeletedRevisionsInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public void Download(DownloadInput input)
		{
			return;
		}

		/// <inheritdoc/>
		public EditResult Edit(EditInput input)
		{
			ThrowNull(input, nameof(input));
			return new EditResult("Success", input.PageId, input.Title ?? "Test Page", EditFlags.NoChange, input.ContentModel, 1, 1, this.CurrentTimestamp, new Dictionary<string, string>());
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
		public HelpResult Help(HelpInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public PageSetResult<ImageRotateItem> ImageRotate(ImageRotateInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<ImportItem> Import(ImportInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public void Initialize()
		{
			if (this.CurrentUserInfo == null)
			{
				this.CurrentUserInfo = new UserInfoResult(
					baseUser: new UserItem(
						userId: 0,
						name: "192.0.2.1",
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
					options: new Dictionary<string, object>(),
					preferencesToken: null,
					rateLimits: new Dictionary<string, RateLimitsItem?>(),
					realName: null,
					unreadText: null);
			}

			return;
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
			ThrowNull(input, nameof(input));
			this.CurrentUserInfo = new UserInfoResult(
				baseUser: new UserItem(
					userId: 1,
					name: input.UserName,
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
				options: new Dictionary<string, object>(),
				preferencesToken: null,
				rateLimits: new Dictionary<string, RateLimitsItem?>(),
				realName: null,
				unreadText: null);
			this.Initialize();

			return LoginResult.AlreadyLoggedIn(this.CurrentUserInfo.UserId, this.CurrentUserInfo.Name);
		}

		/// <inheritdoc/>
		public void Logout()
		{
			return;
		}

		/// <inheritdoc/>
		public ManageTagsResult ManageTags(ManageTagsInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public MergeHistoryResult MergeHistory(MergeHistoryInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<MoveItem> Move(MoveInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public IReadOnlyList<OpenSearchItem> OpenSearch(OpenSearchInput input) => throw new NotImplementedException();

		/// <inheritdoc/>
		public void Options(OptionsInput input)
		{
			return;
		}

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
		public SiteInfoResult SiteInfo(SiteInfoInput input) => throw new NotImplementedException();

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

		/// <summary>Raises the <see cref="Initialized" /> event.</summary>
		/// <param name="e">The <see cref="InitializedEventArgs"/> instance containing the event data.</param>
		protected virtual void OnInitialized(InitializedEventArgs e) => this.Initialized?.Invoke(this, e);

		/// <summary>Raises the <see cref="Initializing" /> event.</summary>
		/// <param name="e">The <see cref="InitializedEventArgs"/> instance containing the event data.</param>
		protected virtual void OnInitializing(InitializingEventArgs e) => this.Initializing?.Invoke(this, e);
		#endregion
	}
}
