﻿namespace RobinHood70.WallE.Base
{
	/* The documentation in this and class is somewhat minimal and repetitive; documentation in the various input, result, and module classes is virtually non-existent. This is due to the fact that much of it is already documented on MediaWiki's website. I have provided links to the relevant pages here in favour of wasting time documenting a lot of classes with duplicate information. */
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.WallE.Design;
	using RobinHood70.WikiCommon;

	#region Public Delegates

	/// <summary>Delegate for title object creation.</summary>
	/// <typeparam name="T">The type of title object to create.</typeparam>
	/// <param name="ns">The namespace.</param>
	/// <param name="title">The page title.</param>
	/// <param name="pageId">The page identifier.</param>
	/// <returns>An <see cref="ITitle"/> object. In external use, this will normally be a <see cref="PageItem"/> or derivative.</returns>
	public delegate T TitleCreator<T>(int ns, string title, long pageId)
		where T : ITitle;
	#endregion

	#region Public Enumerations

	/// <summary>Indicates which methods of stopping the bot should be allowed.</summary>
	[Flags]
	public enum StopCheckMethods
	{
		/// <summary>No stop checking.</summary>
		None = 0,

		/// <summary>Asserts that the logged in user is either a bot or a user. See <see cref="IWikiAbstractionLayer.Assert" />for more information.</summary>
		Assert = 1,

		/// <summary>Check that the bot's username matches the name of the current user.</summary>
		UserNameCheck = 1 << 1,

		/// <summary>Check that the user's talk page has not been changed.</summary>
		/// <remarks>In the API version of this layer, the check is integrated with the existing query and does not require a separate request. Similar functionality might apply with index.php, but would probably also extend to TalkCheckNonQuery.</remarks>
		TalkCheckQuery = 1 << 2,

		/// <summary>Check that the user's talk page has not been changed on non-query actions.</summary>
		/// <remarks>This check cannot be integrated in the API version of this layer at present, so will issue an additional UserInfo request for each action in order to check the talk page. Index.php may not be subject to the same limitations.</remarks>
		TalkCheckNonQuery = 1 << 3,

		/// <summary>Allows the user to stop the bot based on a custom check. See <see cref="IWikiAbstractionLayer.CustomStopCheck" /> for more information.</summary>
		Custom = 1 << 4,

		/// <summary>Use all available stop check methods.</summary>
		All = Assert | UserNameCheck | TalkCheckNonQuery | TalkCheckNonQuery | Custom
	}
	#endregion

	/// <summary>An interface for abstraction layers to implement most MediaWiki functionality.</summary>
	/// <remarks>While centered around the API, the intent is that alternate versions could be created that implement the same or reduced feature set using other methods, such as using index.php or a direct-to-database layer.</remarks>
	[SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Not much to be done while maintaining the ease of abstraction (e.g., using index.php or a database layer). If anyone has a better design, I'm all ears!")]
	public interface IWikiAbstractionLayer
	{
		#region Events

		/// <summary>Occurs when a Captcha check is generated by the wiki.</summary>
		event StrongEventHandler<IWikiAbstractionLayer, CaptchaEventArgs>? CaptchaChallenge;

		/// <summary>Occurs after initialization data has been loaded and processed.</summary>
		/// <remarks>Subscribers to this event should assume that they may get more information back than what they requested (e.g., all interwiki info instead of local only), and filter out any data that they do not require.</remarks>
		event StrongEventHandler<IWikiAbstractionLayer, InitializationEventArgs>? Initialized;

		/// <summary>Occurs when the wiki is about to load initialization data.</summary>
		/// <remarks>Subscribers to this event have the opportunity to request additional SiteInfo data over what the base abstraction layer and any other layers require. It is the subscriber's responsibility to ensure that they're always requesting a superset of the information required and do not inadvertently prevent another subscriber from getting the information it requires. Most settings should be OR'd together with previous values and Filter settings should be set to Any in the event of a conflict. If there's a conflict in the interwiki language code, you will not be able to use co-initialization and should issue a separate SiteInfo request.</remarks>
		event StrongEventHandler<IWikiAbstractionLayer, InitializationEventArgs>? Initializing;

		/// <summary>Occurs when a warning is issued by the wiki.</summary>
		event StrongEventHandler<IWikiAbstractionLayer, WarningEventArgs>? WarningOccurred;
		#endregion

		#region Properties

		/// <summary>Gets or sets the assert string.</summary>
		/// <value>The assert string to be used, such as "bot" or "user" for <c>assert=bot</c> or <c>assert=user</c>.</value>
		string? Assert { get; set; }

		/// <summary>Gets the most recent timestamp from the wiki, which can be used to indicate when an edit was started.</summary>
		/// <value>The current timestamp.</value>
		DateTime? CurrentTimestamp { get; }

		/// <summary>Gets or sets the custom stop check function.</summary>
		/// <value>A function which returns true if the bot should stop what it's doing.</value>
		Func<bool>? CustomStopCheck { get; set; }

		/// <summary>Gets various site information flags.</summary>
		/// <value>The flags. See <see cref="SiteInfoFlags" />.</value>
		SiteInfoFlags Flags { get; }

		/// <summary>Gets or sets the site language code.</summary>
		/// <value>The language code.</value>
		string? LanguageCode { get; set; }

		/// <summary>Gets the name of the site.</summary>
		/// <value>The name of the site.</value>
		string? SiteName { get; }

		/// <summary>Gets the detected site version.</summary>
		/// <value>The MediaWiki version for the site, expressed as an integer (i.e., MW 1.23 = 123).</value>
		/// <remarks>This should not normally need to be set, but is left as settable by derived classes, should customization be needed.</remarks>
		int SiteVersion { get; }

		/// <summary>Gets or sets the various methods to check to see if a stop has been requested.</summary>
		/// <value>The stop methods.</value>
		StopCheckMethods StopCheckMethods { get; set; }

		/// <summary>Gets a value indicating whether the site supports <see href="https://www.mediawiki.org/wiki/Manual:Maxlag_parameter">maxlag checking</see>.</summary>
		/// <value><see langword="true" /> if the site supports <c>maxlag</c> checking; otherwise, <see langword="false" />.</value>
		bool SupportsMaxLag { get; }

		/// <summary>Gets the name of the current user.</summary>
		/// <value>The name of the current user.</value>
		/// <remarks>It's conceivable that not every possible implementor would need a UserName, and it may be null if the user doesn't log in, but it's reasonable to assume that neither of these will be the norm, and UserName is handy to have easily accessible without having to cast to a specific implementor.</remarks>
		string? UserName { get; }

		/// <summary>Gets a list of all warnings.</summary>
		/// <value>The warnings.</value>
		IReadOnlyList<ErrorItem> Warnings { get; }
		#endregion

		#region Support Methods

		/// <summary>Adds a warning to the warning list.</summary>
		/// <param name="code">The code returned by the wiki.</param>
		/// <param name="info">The informative text returned by the wiki.</param>
		void AddWarning(string code, string info);

		/// <summary>Clears the warning list.</summary>
		void ClearWarnings();

		/// <summary>Initializes any needed information without trying to login.</summary>
		void Initialize();

		/// <summary>Determines whether the relevant interface for this abstraction layer is enabled on the current wiki.</summary>
		/// <returns><see langword="true" /> if the interface is enabled; otherwise, <see langword="false" />.</returns>
		/// <remarks>This function will normally need to communicate with the wiki to determine the return value. Since that consumes significantly more time than a simple property check, it's implemented as a function rather than a property.</remarks>
		[SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Performs a time-consuming operation.")]
		bool IsEnabled();
		#endregion

		#region Wiki Methods

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Allcategories">Allcategories</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of categories.</returns>
		IReadOnlyList<AllCategoriesItem> AllCategories(AllCategoriesInput input);

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Alldeletedrevisions">Alldeletecrevisions</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of deleted revisions.</returns>
		IReadOnlyList<AllRevisionsItem> AllDeletedRevisions(AllDeletedRevisionsInput input);

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Allimages">Allimages</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of images.</returns>
		IReadOnlyList<AllImagesItem> AllImages(AllImagesInput input);

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Alllinks">Allfileusages</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of file usage links.</returns>
		IReadOnlyList<AllLinksItem> AllFileUsages(AllFileUsagesInput input);

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Alllinks">Alllinks</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of links.</returns>
		IReadOnlyList<AllLinksItem> AllLinks(AllLinksInput input);

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Allmessages">Allmessages</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of messages.</returns>
		IReadOnlyList<AllMessagesItem> AllMessages(AllMessagesInput input);

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Allpages">Allpages</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of page titles.</returns>
		IReadOnlyList<WikiTitleItem> AllPages(AllPagesInput input);

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Alllinks">Allredirects</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of redirect links.</returns>
		IReadOnlyList<AllLinksItem> AllRedirects(AllRedirectsInput input);

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Allrevisions">Allrevisions</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of revisions.</returns>
		IReadOnlyList<AllRevisionsItem> AllRevisions(AllRevisionsInput input);

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Alllinks">Alltransclusions</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of transclusions (as links).</returns>
		IReadOnlyList<AllLinksItem> AllTransclusions(AllTransclusionsInput input);

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Allusers">Allusers</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of users.</returns>
		IReadOnlyList<AllUsersItem> AllUsers(AllUsersInput input);

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Backlinks">Backlinks</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of links.</returns>
		IReadOnlyList<BacklinksItem> Backlinks(BacklinksInput input);

		/// <summary>Blocks a user. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Block">Block</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the block.</returns>
		BlockResult Block(BlockInput input);

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Blocks">Blocks</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of blocks.</returns>
		IReadOnlyList<BlocksResult> Blocks(BlocksInput input);

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Categorymembers">Categorymembers</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of category members.</returns>
		IReadOnlyList<CategoryMembersItem> CategoryMembers(CategoryMembersInput input);

		/// <summary>Checks a token. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Checktoken">Checktoken</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the checked token.</returns>
		CheckTokenResult CheckToken(CheckTokenInput input);

		/// <summary>Clears the user's "has message" flag. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Clearhasmsg">Clearhasmsg</see> API module.</summary>
		/// <returns>Whether the attempt was successful.</returns>
		bool ClearHasMessage();

		/// <summary>Compares two revisions or pages. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Compare">Compare</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the comparison.</returns>
		CompareResult Compare(CompareInput input);

		/// <summary>Creates an account. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Createaccount">Createaccount</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the account created.</returns>
		CreateAccountResult CreateAccount(CreateAccountInput input);

		/// <summary>Deletes a page. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Delete">Delete</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the deletion.</returns>
		DeleteResult Delete(DeleteInput input);

		/// <summary>Downloads the specified resource (usually a URI) to a file.</summary>
		/// <param name="input">The input parameters.</param>
		void Download(DownloadInput input);

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Deletedrevisions">Deletedrevisions</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of deleted revisions.</returns>
		IReadOnlyList<DeletedRevisionsItem> DeletedRevisions(ListDeletedRevisionsInput input);

		/// <summary>Edits a page. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Edit">Edit</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the edit.</returns>
		EditResult Edit(EditInput input);

		/// <summary>Emails a user. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Emailuser">Emailuser</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the e-mail that was sent.</returns>
		EmailUserResult EmailUser(EmailUserInput input);

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Expandtemplates">Expandtemplates</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the expanded templates.</returns>
		ExpandTemplatesResult ExpandTemplates(ExpandTemplatesInput input);

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Externalurlusage">Externalurlusage</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of external URLs and the pages they're used on.</returns>
		IReadOnlyList<ExternalUrlUsageItem> ExternalUrlUsage(ExternalUrlUsageInput input);

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Feedcontributions">Feedcontributions</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>The raw XML of the Contributions RSS feed.</returns>
		string FeedContributions(FeedContributionsInput input);

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Feedrecentchanges">Feedrecentchanges</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>The raw XML of the Recent Changes RSS feed.</returns>
		string FeedRecentChanges(FeedRecentChangesInput input);

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Feedwatchlist">Feedwatchlist</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>The raw XML of the Watchlist RSS feed.</returns>
		string FeedWatchlist(FeedWatchlistInput input);

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Filearchive">Filearchive</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of file archives.</returns>
		IReadOnlyList<FileArchiveItem> FileArchive(FileArchiveInput input);

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Filerepoinfo">Filerepoinfo</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of information for each repository.</returns>
		IReadOnlyList<FileRepositoryInfoItem> FileRepositoryInfo(FileRepositoryInfoInput input);

		/// <summary>Reverts a file to an older version. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Filerevert">Filerevert</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the file reversion.</returns>
		FileRevertResult FileRevert(FileRevertInput input);

		/// <summary>Gets help information. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Help">Help</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>The API help for the module(s) requested.</returns>
		HelpResult Help(HelpInput input);

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Imagerotate">Imagerotate</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of page titles with image rotation information.</returns>
		PageSetResult<ImageRotateItem> ImageRotate(ImageRotateInput input);

		/// <summary>Imports pages into a wiki. Correspondes to the <see href="https://www.mediawiki.org/wiki/API:Import">Import</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of page titles with import information.</returns>
		IReadOnlyList<ImportItem> Import(ImportInput input);

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Iwbacklinks">Iwbacklinks</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of page titles with interwiki backlink information.</returns>
		IReadOnlyList<InterwikiBacklinksItem> InterwikiBacklinks(InterwikiBacklinksInput input);

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Langbacklinks">Langbacklinks</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of page titles with language backlink information.</returns>
		IReadOnlyList<LanguageBacklinksItem> LanguageBacklinks(LanguageBacklinksInput input);

		/// <summary>Loads page information. Incorporates the various API <see href="https://www.mediawiki.org/wiki/API:Properties">property</see> modules.</summary>
		/// <param name="pageSetInput">A pageset input which specifies a list of page titles, page IDs, revision IDs, or a generator.</param>
		/// <param name="propertyInputs"><para>A collection of any combination of property inputs. Built-in property inputs include: <see cref="CategoriesInput" />, <see cref="CategoryInfoInput" />, <see cref="ContributorsInput" />, <see cref="DeletedRevisionsInput" />, <see cref="DuplicateFilesInput" />, <see cref="ExternalLinksInput" />, <see cref="FileUsageInput" />, <see cref="ImageInfoInput" />, <see cref="ImagesInput" />, <see cref="InfoInput" />, <see cref="InterwikiLinksInput" />, <see cref="LanguageLinksInput" />, <see cref="LinksHereInput" />, <see cref="PagePropertiesInput" />, <see cref="RedirectsInput" />, <see cref="RevisionsInput" />, <see cref="StashImageInfoInput" />, and <see cref="TranscludedInInput" />.</para>
		/// <para>A typical, simple collection would include an InfoInput and a RevisionsInput, which would fetch basic information about the page, along with the latest revision.</para></param>
		/// <returns>A list of pages based on the pageSetInput parameter with the information for each of the property inputs.</returns>
		PageSetResult<PageItem> LoadPages(QueryPageSetInput pageSetInput, IEnumerable<IPropertyInput> propertyInputs);

		/// <summary>Loads page information. Incorporates the various API <see href="https://www.mediawiki.org/wiki/API:Properties">property</see> modules.</summary>
		/// <param name="pageSetInput">A pageset input which specifies a list of page titles, page IDs, revision IDs, or a generator.</param>
		/// <param name="propertyInputs"><para>A collection of any combination of property inputs. Built-in property inputs include: <see cref="CategoriesInput" />, <see cref="CategoryInfoInput" />, <see cref="ContributorsInput" />, <see cref="DeletedRevisionsInput" />, <see cref="DuplicateFilesInput" />, <see cref="ExternalLinksInput" />, <see cref="FileUsageInput" />, <see cref="ImageInfoInput" />, <see cref="ImagesInput" />, <see cref="InfoInput" />, <see cref="InterwikiLinksInput" />, <see cref="LanguageLinksInput" />, <see cref="LinksHereInput" />, <see cref="PagePropertiesInput" />, <see cref="RedirectsInput" />, <see cref="RevisionsInput" />, <see cref="StashImageInfoInput" />, and <see cref="TranscludedInInput" />.</para>
		/// <para>A typical, simple collection would include an InfoInput and a RevisionsInput, which would fetch basic information about the page, along with the latest revision.</para></param>
		/// <param name="pageFactory">A factory method which creates an object derived from PageItem.</param>
		/// <returns>A list of pages based on the <paramref name="pageSetInput" /> parameter with the information determined by each of the property inputs.</returns>
		// Making this a generic method looked very nice from this end, but caused no end of headaches from the client end, so I switched it to a factory-based call instead.
		PageSetResult<PageItem> LoadPages(QueryPageSetInput pageSetInput, IEnumerable<IPropertyInput> propertyInputs, TitleCreator<PageItem> pageFactory);

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Logevents">Logevents</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of log events. The specific class used for each event will vary depending on the event itself.</returns>
		IReadOnlyList<LogEventsItem> LogEvents(LogEventsInput input);

		/// <summary>Logs the user in. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Login">Login</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the login.</returns>
		LoginResult Login(LoginInput input);

		/// <summary>Logs the user out. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Logout">Logout</see> API module.</summary>
		// The return value from the wiki is an empty object, so this is a void function.
		void Logout();

		/// <summary>Adds, removes, activates, or deactivates a page tag. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Managetags">Managetags</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the altered tag.</returns>
		ManageTagsResult ManageTags(ManageTagsInput input);

		/// <summary>Merges the history of two pages. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Mergehistory">Mergehistory</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the merge.</returns>
		MergeHistoryResult MergeHistory(MergeHistoryInput input);

		/// <summary>Moves a page, and optionally, it's talk/sub-pages. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Move">Move</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of results for each page, subpage, or talk page moved, including errors that may indicate only partial success.</returns>
		/// <remarks>Due to the fact that this method can generate multiple errors, any errors returned here will not be raised as exceptions. Results should instead be scanned for errors, and acted upon accordingly.</remarks>
		MoveResult Move(MoveInput input);

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Opensearch">Opensearch</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of open search results.</returns>
		/// <seealso cref="PrefixSearch" />
		/// <seealso cref="Search" />
		IReadOnlyList<OpenSearchItem> OpenSearch(OpenSearchInput input);

		/// <summary>Sets one or more options. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Options">Options</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <remarks>The MediaWiki return value is hard-coded to "success" and is therefore useless, so this is a void function.</remarks>
		void Options(OptionsInput input);

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Pagepropnames">Pagepropnames</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of proerty names.</returns>
		IReadOnlyList<string> PagePropertyNames(PagePropertyNamesInput input);

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Pageswithprop">Pageswithprop</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of page titles along with the value of the property on that page.</returns>
		IReadOnlyList<PagesWithPropertyItem> PagesWithProperty(PagesWithPropertyInput input);

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Paraminfo">Paraminfo</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A dictionary of parameter information with the module name as the key.</returns>
		IReadOnlyDictionary<string, ParameterInfoItem> ParameterInfo(ParameterInfoInput input);

		/// <summary>Parses custom text, a page, or a revision. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Parse">Parse</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>The result of the parse.</returns>
		ParseResult Parse(ParseInput input);

		/// <summary>Patrols a recent change or revision. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Patrol">Patrol</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>The patrolled page information along with the Recent Changes ID.</returns>
		PatrolResult Patrol(PatrolInput input);

		/// <summary>Searches one or more namespaces for page titles prefixed by the given characters. Unlike AllPages, the search pattern is not considered rigid, and may include parsing out definite articles or similar modifications. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Prefixsearch">Prefixsearch</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of page titles matching the prefix.</returns>
		/// <seealso cref="OpenSearch" />
		/// <seealso cref="Search" />
		IReadOnlyList<WikiTitleItem> PrefixSearch(PrefixSearchInput input);

		/// <summary>Protects a page. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Protect">Protect</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the protection applied.</returns>
		ProtectResult Protect(ProtectInput input);

		/// <summary>Retrieves page titles that are creation-protected. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Protectedtitles">Protectedtitles</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of creation-protected articles.</returns>
		IReadOnlyList<ProtectedTitlesItem> ProtectedTitles(ProtectedTitlesInput input);

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Purge">Purge</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of pages that were purged, along with information about the purge for each page.</returns>
		PageSetResult<PurgeItem> Purge(PurgeInput input);

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Querypage">Querypage</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of pages titles and, when available, the related value. Other fields will be returned as a set of name-value pairs in the <see cref="QueryPageItem.DatabaseResult" /> dictionary.</returns>
		QueryPageResult QueryPage(QueryPageInput input);

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Random">Random</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A random list of page titles.</returns>
		IReadOnlyList<WikiTitleItem> Random(RandomInput input);

		/// <summary>Returns data corresponding to the <see href="https://www.mediawiki.org/wiki/API:Recentchanges">Recentchanges</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of recent changes.</returns>
		IReadOnlyList<RecentChangesItem> RecentChanges(RecentChangesInput input);

		/// <summary>Resets a user's password. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Resetpassword">Resetpassword</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the attempt to reset the password.</returns>
		ResetPasswordResult ResetPassword(ResetPasswordInput input);

		/// <summary>Hides one or more revisions from those without permission to view them. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Revisiondelete"></see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the deleted revisions.</returns>
		RevisionDeleteResult RevisionDelete(RevisionDeleteInput input);

		/// <summary>Rolls back all of the last user's edits to a page. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Rollback">Rollback</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the rollback.</returns>
		RollbackResult Rollback(RollbackInput input);

		/// <summary>Returns Really Simple Discovery information. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Rsd">Rsd</see> API module.</summary>
		/// <returns>The raw XML of the RSD schema.</returns>
		string Rsd();

		/// <summary>Searches for wiki pages that fulfil given criteria. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Search">Search</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of page titles fulfilling the criteria, along with information about the search hit on each page.</returns>
		/// <seealso cref="OpenSearch" />
		/// <seealso cref="PrefixSearch" />
		SearchResult Search(SearchInput input);

		/// <summary>Sets the notification timestamp for watched pages, marking revisions as being read/unread. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Setnotificationtimestamp">Setnotificationtimestamp</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of page titles along with various informaiton about the change.</returns>
		PageSetResult<SetNotificationTimestampItem> SetNotificationTimestamp(SetNotificationTimestampInput input);

		/// <summary>Returns information about the site. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Siteinfo">Siteinfo</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>The requested site information.</returns>
		SiteInfoResult SiteInfo(SiteInfoInput input);

		/// <summary>Returns information about <see href="https://www.mediawiki.org/wiki/Manual:UploadStash">stashed</see> files. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Stashimageinfo">Stashimageinfo</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of image information for each image or chunk.</returns>
		IReadOnlyList<ImageInfoItem> StashImageInfo(StashImageInfoInput input);

		/// <summary>Adds or removes tags based on revision IDs, log IDs, or Recent Changes IDs. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Tag">Tag</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the tag/untag.</returns>
		IReadOnlyList<TagItem> Tag(TagInput input);

		/// <summary>Displays information about all tags available on the wiki. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Tags">Tags</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of tag information.</returns>
		IReadOnlyList<TagsItem> Tags(TagsInput input);

		/// <summary>Unblocks a user. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Unblock">Unblock</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the unblock operation and the user affected.</returns>
		UnblockResult Unblock(UnblockInput input);

		/// <summary>Undeletes a page or specific revisions thereof (by file ID or date/time). Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Undelete">Undelete</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the undeleted page.</returns>
		UndeleteResult Undelete(UndeleteInput input);

		/// <summary>Uploads a file to the wiki. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Upload">Upload</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the uploaded file.</returns>
		UploadResult Upload(UploadInput input);

		/// <summary>Retrieves a user's contributions. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Usercontribs">Usercontribs</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of the user's contributions.</returns>
		IReadOnlyList<UserContributionsItem> UserContributions(UserContributionsInput input);

		/// <summary>Returns information about the current user. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Userinfo">Userinfo</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the current user.</returns>
		UserInfoResult UserInfo(UserInfoInput input);

		/// <summary>Adds or removes user rights (based on rights groups). Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Userrights">Userrights</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>The user name and ID, and the groups they were added to or removed from.</returns>
		UserRightsResult UserRights(UserRightsInput input);

		/// <summary>Retrieves information about specific users on the wiki. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Users">Users</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about the users.</returns>
		IReadOnlyList<UsersItem> Users(UsersInput input);

		/// <summary>Watches or unwatches pages for the current user. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Watch">Watch</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of page titles and whether they were watched, unwatched, or not found.</returns>
		PageSetResult<WatchItem> Watch(WatchInput input);

		/// <summary>Retrieve detailed information about the current user's watchlist. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Watchlist">Watchlist</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>Information about each page on the user's watchlist.</returns>
		IReadOnlyList<WatchlistItem> Watchlist(WatchlistInput input);

		/// <summary>Retrieve some or all of the current user's watchlist. Corresponds to the <see href="https://www.mediawiki.org/wiki/API:Watchlistraw">Watchlistraw</see> API module.</summary>
		/// <param name="input">The input parameters.</param>
		/// <returns>A list of page titles in the user's watchlist, and whether or not they've been changed.</returns>
		IReadOnlyList<WatchlistRawItem> WatchlistRaw(WatchlistRawInput input);
		#endregion
	}
}