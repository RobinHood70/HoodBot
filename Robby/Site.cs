namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using System.Net;
	using System.Runtime.CompilerServices;
	using System.Text.RegularExpressions;
	using Design;
	using Pages;
	using WallE.Base;
	using WikiCommon;
	using static Properties.Resources;
	using static WikiCommon.Globals;

	/// <summary>Represents a single wiki site.</summary>
	/// <seealso cref="RobinHood70.Robby.IMessageSource" />
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Sufficiently maintainable for now. Could conceivably split off the GetX() methods if needed, I suppose.")]
	public class Site : IMessageSource
	{
		#region Fields
		private readonly Dictionary<string, MagicWord> magicWords = new Dictionary<string, MagicWord>();
		private string articlePath;
		private CultureInfo culture = CultureInfo.CurrentCulture;
		private HashSet<Title> disambiguationTemplates = null;
		private Regex redirectTargetFinder;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="Site"/> class.</summary>
		/// <param name="wiki">The <see cref="IWikiAbstractionLayer"/> to use. This controls whether the API is used or some other access method.</param>
		public Site(IWikiAbstractionLayer wiki)
		{
			this.DefaultLoadOptions = PageLoadOptions.Default;
			this.PageCreator = new DefaultPageCreator();
			this.AbstractionLayer = wiki;
			this.AbstractionLayer.WarningOccurred += this.Wiki_WarningOccurred;
		}
		#endregion

		#region Events

		/// <summary>Occurs when an edit is ignored due to the <see cref="AllowEditing"/> flag being false.</summary>
		public event StrongEventHandler<Site, EditIgnoredEventArgs> EditIgnored;

		/// <summary>Occurs when a warning should be sent to the user.</summary>
		/// <remarks>This functions as an aggregate event, so only the Site object will ever raise a warning. Warnings should be explicit enough about what occurred to determine any other information required. The true sending object is available as part of the event arguments if needed.</remarks>
		public event StrongEventHandler<Site, WarningEventArgs> WarningOccurred;
		#endregion

		#region Public Properties

		/// <summary>Gets the wiki abstraction layer.</summary>
		/// <value>The wiki abstraction layer.</value>
		public IWikiAbstractionLayer AbstractionLayer { get; }

		/// <summary>Gets or sets a value indicating whether methods that would alter the wiki should be allowed.</summary>
		/// <remarks>If set to false, most methods will silently fail, indicating success whenever possible.</remarks>
		public bool AllowEditing { get; set; } = true;

		/// <summary>Gets a value indicating whether the first letter of titles is case-sensitive.</summary>
		/// <value><c>true</c> if the first letter of titles is case-sensitive; otherwise, <c>false</c>.</value>
		public bool CaseSensitive { get; private set; }

		/// <summary>Gets or sets a CultureInfo object base the wiki's language and variant.</summary>
		/// <value>The culture of the wiki.</value>
		/// <remarks>Not all languages available in MediaWiki have direct equivalents in Windows. The bot will attempt to fall back to the more general language or variant when possible, but this property is left settable in the event that the choice made is unacceptable. If the culture cannot be determined, <see cref="CultureInfo.CurrentCulture"/> is used instead. Attempting to set the Culture to null will also result in CurrentCulture being used.</remarks>
		public CultureInfo Culture
		{
			get => this.culture;
			set => this.culture = value ?? CultureInfo.CurrentCulture;
		}

		/// <summary>Gets or sets the default load options.</summary>
		/// <value>The default load options.</value>
		/// <remarks>If you need to detect disambiguations, you should consider setting this to include Properties for wikis using Disambiguator or Templates for those that aren't.</remarks>
		public PageLoadOptions DefaultLoadOptions { get; set; }

		/// <summary>Gets the list of disambiguation templates on wikis that aren't using Disambiguator.</summary>
		/// <value>The disambiguation templates.</value>
		/// <remarks>This will be auto-populated on first use if not already set.</remarks>
		public IEnumerable<Title> DisambiguationTemplates => this.disambiguationTemplates ?? this.LoadDisambiguationTemplates();

		/// <summary>Gets a value indicating whether the Disambiguator extension is available.</summary>
		/// <value><c>true</c> if the Disambiguator extension is available; otherwise, <c>false</c>.</value>
		public bool DisambiguatorAvailable { get; private set; }

		/// <summary>Gets a list of current magic words on the site.</summary>
		/// <value>The magic words.</value>
		public IReadOnlyDictionary<string, MagicWord> MagicWords => this.magicWords;

		/// <summary>Gets the site name.</summary>
		/// <value>The name of the site.</value>
		public string Name { get; private set; }

		/// <summary>Gets the site namespaces.</summary>
		/// <value>The site namespaces.</value>
		public NamespaceCollection Namespaces { get; private set; }

		/// <summary>Gets or sets the page creator.</summary>
		/// <value>The page creator.</value>
		/// <remarks>A PageCreator is an abstract factory which serves as a bridge between customized PageItem types from WallE and the corresponding custom Page type for Robby.</remarks>
		public PageCreator PageCreator { get; set; }

		/// <summary>Gets the name of the server—typically, the base URL.</summary>
		/// <value>The name of the server.</value>
		public string ServerName { get; private set; }

		/// <summary>Gets the bot's user name.</summary>
		/// <value>The bot's user name.</value>
		public string UserName { get; private set; }

		/// <summary>Gets the MediaWiki version of the wiki.</summary>
		/// <value>The MediaWiki version of the wiki.</value>
		public string Version { get; private set; }
		#endregion

		#region Public Static Methods

		/// <summary>A convenience method for debugging, this simply outputs all warnings to any Debug trace listeners (e.g., the Debug Output window).</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="WarningEventArgs"/> instance containing the event data.</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "RobinHood70.Robby.Globals.CurrentCulture(System.String,System.Object[])", Justification = "I'm allowing English only here because it's only intended for debugging.")]
		public static void DebugWarningEventHandler(Site sender, WarningEventArgs e)
		{
			ThrowNull(sender, nameof(sender));
			ThrowNull(e, nameof(e));
			Debug.WriteLine(CurrentCulture(Warning, e.Sender.GetType(), e.Warning), sender.ToString());
		}
		#endregion

		#region Public Methods

		/// <summary>Downloads a resource to a local file.</summary>
		/// <param name="resource">The location of the resource (typically, the a Uri path). This does <em>not</em> have to be located on the wiki.</param>
		/// <param name="fileName">Name of the file.</param>
		/// <remarks><paramref name="resource"/> is not a <see cref="Uri"/> in order to satisfy <see cref="IWikiAbstractionLayer"/>'s agnosticism. In practice, however, it will almost certainly always be one.</remarks>
		public void Download(string resource, string fileName) => this.Download(new DownloadInput(resource, fileName));

		/// <summary>Downloads the most recent version of a file from the site.</summary>
		/// <param name="pageName">Name of the page. You do not have to specify the File namespace, but you may if it's convenient.</param>
		/// <param name="fileName">Name of the file.</param>
		public void DownloadFile(string pageName, string fileName)
		{
			var fileTitle = new TitleCollection(this, MediaWikiNamespaces.File, pageName);
			var filePages = fileTitle.Load(PageModules.FileInfo);
			if (filePages.Count == 1 && filePages[0] is FilePage filePage)
			{
				filePage.Download(fileName);
			}
		}

		/// <summary>Gets a message from MediaWiki space.</summary>
		/// <param name="message">The message.</param>
		/// <param name="arguments">Optional arguments to substitute into the message.</param>
		/// <returns>The text of the message.</returns>
		public string GetMessage(string message, params string[] arguments) => this.GetMessage(message, arguments as IEnumerable<string>);

		/// <summary>Gets a message from MediaWiki space.</summary>
		/// <param name="message">The message.</param>
		/// <param name="arguments">Optional arguments to substitute into the message.</param>
		/// <returns>The text of the message.</returns>
		public string GetMessage(string message, IEnumerable<string> arguments)
		{
			var messages = this.GetMessages(new[] { message }, arguments);
			return messages[message].Text;
		}

		/// <summary>Gets multiple messages from MediaWiki space.</summary>
		/// <param name="messages">The messages.</param>
		/// <param name="arguments">Optional arguments to substitute into the messages.</param>
		/// <returns>The text of the message.</returns>
		public IReadOnlyDictionary<string, Message> GetMessages(IEnumerable<string> messages, params string[] arguments) => this.GetMessages(messages, arguments as IEnumerable<string>);

		/// <summary>This is a convenience method to quickly get the text of a single page.</summary>
		/// <param name="pageName">Name of the page.</param>
		/// <returns>The text of the page.</returns>
		public string GetPageText(string pageName)
		{
			var result = new TitleCollection(this, pageName).Load();
			return result.Count == 1 ? result[0].Text : null;
		}

		/// <summary>Gets a message from MediaWiki space with any magic words and the like parsed into text.</summary>
		/// <param name="message">The message.</param>
		/// <param name="arguments">Optional arguments to substitute into the message.</param>
		/// <returns>The text of the message.</returns>
		public string GetParsedMessage(string message, IEnumerable<string> arguments) => this.GetParsedMessage(message, arguments, null);

		/// <summary>Gets a message from MediaWiki space with any magic words and the like parsed into text.</summary>
		/// <param name="message">The message.</param>
		/// <param name="arguments">Optional arguments to substitute into the message.</param>
		/// <param name="title">The title to use for parsing.</param>
		/// <returns>The text of the message.</returns>
		public string GetParsedMessage(string message, IEnumerable<string> arguments, Title title)
		{
			var messages = this.GetParsedMessages(new[] { message }, arguments, title);
			return messages[message].Text;
		}

		/// <summary>Gets multiple messages from MediaWiki space with any magic words and the like parsed into text.</summary>
		/// <param name="messages">The messages.</param>
		/// <param name="arguments">Optional arguments to substitute into the message.</param>
		/// <returns>The text of the message.</returns>
		public IReadOnlyDictionary<string, Message> GetParsedMessages(IEnumerable<string> messages, IEnumerable<string> arguments) => this.GetParsedMessages(messages, arguments, null);

		/// <summary>Gets the recent changes.</summary>
		/// <returns></returns>
		/// <autogeneratedoc />
		public IReadOnlyList<RecentChange> GetRecentChanges() => this.GetRecentChanges(new RecentChangesOptions());

		/// <summary>Gets the recent changes.</summary>
		/// <param name="namespaces">The namespaces.</param>
		/// <returns></returns>
		/// <autogeneratedoc />
		public IReadOnlyList<RecentChange> GetRecentChanges(IEnumerable<int> namespaces) => this.GetRecentChanges(new RecentChangesOptions() { Namespaces = namespaces });

		/// <summary>Gets the recent changes.</summary>
		/// <param name="tag">The tag.</param>
		/// <returns></returns>
		/// <autogeneratedoc />
		public IReadOnlyList<RecentChange> GetRecentChanges(string tag) => this.GetRecentChanges(new RecentChangesOptions() { Tag = tag });

		/// <summary>Gets the recent changes.</summary>
		/// <param name="types">The types.</param>
		/// <returns></returns>
		/// <autogeneratedoc />
		public IReadOnlyList<RecentChange> GetRecentChanges(RecentChangesTypes types) => this.GetRecentChanges(new RecentChangesOptions() { Types = types });

		/// <summary>Gets the recent changes.</summary>
		/// <param name="anonymous">The anonymous.</param>
		/// <param name="bots">The bots.</param>
		/// <param name="minor">The minor.</param>
		/// <param name="patrolled">The patrolled.</param>
		/// <param name="redirects">The redirects.</param>
		/// <returns></returns>
		/// <autogeneratedoc />
		public IReadOnlyList<RecentChange> GetRecentChanges(Filter anonymous, Filter bots, Filter minor, Filter patrolled, Filter redirects) => this.GetRecentChanges(new RecentChangesOptions() { Anonymous = anonymous, Bots = bots, Minor = minor, Patrolled = patrolled, Redirects = redirects });

		/// <summary>Gets the recent changes.</summary>
		/// <param name="anonymous">The anonymous.</param>
		/// <param name="bots">The bots.</param>
		/// <param name="minor">The minor.</param>
		/// <param name="patrolled">The patrolled.</param>
		/// <param name="redirects">The redirects.</param>
		/// <param name="types">The types.</param>
		/// <returns></returns>
		/// <autogeneratedoc />
		public IReadOnlyList<RecentChange> GetRecentChanges(Filter anonymous, Filter bots, Filter minor, Filter patrolled, Filter redirects, RecentChangesTypes types) => this.GetRecentChanges(new RecentChangesOptions() { Anonymous = anonymous, Bots = bots, Minor = minor, Patrolled = patrolled, Redirects = redirects, Types = types });

		/// <summary>Gets the recent changes.</summary>
		/// <param name="start">The start.</param>
		/// <param name="end">The end.</param>
		/// <returns></returns>
		/// <autogeneratedoc />
		public IReadOnlyList<RecentChange> GetRecentChanges(DateTime? start, DateTime? end) => this.GetRecentChanges(new RecentChangesOptions() { Start = start, End = end });

		/// <summary>Gets the recent changes.</summary>
		/// <param name="start">The start.</param>
		/// <param name="end">The end.</param>
		/// <param name="anonymous">The anonymous.</param>
		/// <param name="bots">The bots.</param>
		/// <param name="minor">The minor.</param>
		/// <param name="patrolled">The patrolled.</param>
		/// <param name="redirects">The redirects.</param>
		/// <param name="types">The types.</param>
		/// <returns></returns>
		/// <autogeneratedoc />
		public IReadOnlyList<RecentChange> GetRecentChanges(DateTime? start, DateTime? end, Filter anonymous, Filter bots, Filter minor, Filter patrolled, Filter redirects, RecentChangesTypes types) => this.GetRecentChanges(new RecentChangesOptions() { Start = start, End = end, Anonymous = anonymous, Bots = bots, Minor = minor, Patrolled = patrolled, Redirects = redirects, Types = types });

		/// <summary>Gets the recent changes.</summary>
		/// <param name="start">The start.</param>
		/// <param name="newer">if set to <c>true</c> [newer].</param>
		/// <returns></returns>
		/// <autogeneratedoc />
		public IReadOnlyList<RecentChange> GetRecentChanges(DateTime start, bool newer) => this.GetRecentChanges(start, newer, 0);

		/// <summary>Gets the recent changes.</summary>
		/// <param name="start">The start.</param>
		/// <param name="newer">if set to <c>true</c> [newer].</param>
		/// <param name="count">The count.</param>
		/// <returns></returns>
		/// <autogeneratedoc />
		public IReadOnlyList<RecentChange> GetRecentChanges(DateTime start, bool newer, int count) => this.GetRecentChanges(new RecentChangesOptions() { Start = start, Newer = newer, Count = count });

		/// <summary>Gets the recent changes.</summary>
		/// <param name="user">The user.</param>
		/// <param name="exclude">if set to <c>true</c> [exclude].</param>
		/// <returns></returns>
		/// <autogeneratedoc />
		public IReadOnlyList<RecentChange> GetRecentChanges(string user, bool exclude) => this.GetRecentChanges(new RecentChangesOptions() { User = user, ExcludeUser = exclude });

		/// <summary>Gets the user information.</summary>
		/// <param name="users">The users.</param>
		/// <returns></returns>
		/// <autogeneratedoc />
		public IReadOnlyList<User> GetUserInformation(params string[] users) => this.GetUserInformation(users as IEnumerable<string>);

		/// <summary>Gets the users in groups.</summary>
		/// <param name="onlyActiveUsers">if set to <c>true</c> [only active users].</param>
		/// <param name="onlyUsersWithEdits">if set to <c>true</c> [only users with edits].</param>
		/// <param name="groups">The groups.</param>
		/// <returns></returns>
		/// <autogeneratedoc />
		public IReadOnlyList<string> GetUsersInGroups(bool onlyActiveUsers, bool onlyUsersWithEdits, params string[] groups) => this.GetUsersInGroups(onlyActiveUsers, onlyUsersWithEdits, groups as IEnumerable<string>);

		/// <summary>Gets the users with rights.</summary>
		/// <param name="onlyActiveUsers">if set to <c>true</c> [only active users].</param>
		/// <param name="onlyUsersWithEdits">if set to <c>true</c> [only users with edits].</param>
		/// <param name="rights">The rights.</param>
		/// <returns></returns>
		/// <autogeneratedoc />
		public IReadOnlyList<string> GetUsersWithRights(bool onlyActiveUsers, bool onlyUsersWithEdits, params string[] rights) => this.GetUsersWithRights(onlyActiveUsers, onlyUsersWithEdits, rights as IEnumerable<string>);

		/// <summary>Logins the specified user name.</summary>
		/// <param name="userName">Name of the user. This can be null if you wish to edit anonymously.</param>
		/// <param name="password">The password.</param>
		public void Login(string userName, string password) => this.Login(userName, password, null);

		/// <summary>Namespaces from name.</summary>
		/// <param name="fullName">The full name.</param>
		/// <returns></returns>
		/// <autogeneratedoc />
		public Namespace NamespaceFromName(string fullName)
		{
			if (fullName != null)
			{
				var split = fullName.Split(new[] { ':' }, 2);
				if (split.Length == 2 && this.Namespaces.TryGetValue(split[0], out var ns))
				{
					return ns;
				}
			}

			return this.Namespaces[MediaWikiNamespaces.Main];
		}

		/// <summary>Upload a file to the wiki.</summary>
		/// <param name="fileName">The full path and filename of the file to upload.</param>
		/// <param name="editSummary">The edit summary for the upload.</param>
		/// <remarks>The destination filename will be the same as the local filename.</remarks>
		/// <exception cref="ArgumentException">Path contains an invalid character.</exception>
		public void Upload(string fileName, string editSummary) => this.Upload(fileName, null, editSummary, null);

		/// <summary>Upload a file to the wiki.</summary>
		/// <param name="fileName">The full path and filename of the file to upload.</param>
		/// <param name="destinationName">The bare name (i.e., do not include "File:") of the file to upload to on the wiki. Set to null to use the filename from the <paramref name="fileName" /> parameter.</param>
		/// <param name="editSummary">The edit summary for the upload.</param>
		/// <remarks>The destination filename will be the same as the local filename.</remarks>
		/// <exception cref="ArgumentException">Path contains an invalid character.</exception>
		public void Upload(string fileName, string destinationName, string editSummary) => this.Upload(fileName, destinationName, editSummary, null);
		#endregion

		#region Public Virtual Methods

		/// <summary>Clears the message.</summary>
		/// <returns></returns>
		/// <autogeneratedoc />
		public virtual bool ClearMessage()
		{
			if (!this.AllowEditing)
			{
				this.PublishIgnoredEdit(this, null);
				return true;
			}

			return this.AbstractionLayer.ClearHasMessage();
		}

		/// <summary>Gets the article path.</summary>
		/// <param name="articleName">Name of the article.</param>
		/// <returns></returns>
		/// <autogeneratedoc />
		public virtual Uri GetArticlePath(string articleName)
		{
			// Could be done as a one-liner, but split out for easier maintenance and debugging.
			if (string.IsNullOrWhiteSpace(articleName))
			{
				articleName = string.Empty;
			}
			else
			{
				articleName = articleName.Replace(' ', '_');
				articleName = WebUtility.UrlEncode(articleName);
			}

			return new Uri(this.articlePath.Replace("$1", articleName).TrimEnd('/'));
		}

		/// <summary>Gets the blocks.</summary>
		/// <returns></returns>
		/// <autogeneratedoc />
		public virtual IReadOnlyList<Block> GetBlocks() => this.GetBlocks(new BlocksInput() { Properties = BlocksProperties.All });

		/// <summary>Gets the blocks.</summary>
		/// <param name="filterAccount">The filter account.</param>
		/// <param name="filterIP">The filter ip.</param>
		/// <param name="filterRange">The filter range.</param>
		/// <param name="filterTemporary">The filter temporary.</param>
		/// <returns></returns>
		/// <autogeneratedoc />
		public virtual IReadOnlyList<Block> GetBlocks(Filter filterAccount, Filter filterIP, Filter filterRange, Filter filterTemporary) => this.GetBlocks(new BlocksInput() { FilterAccount = filterAccount, FilterIP = filterIP, FilterRange = filterRange, FilterTemporary = filterTemporary, Properties = BlocksProperties.All });

		/// <summary>Gets the blocks.</summary>
		/// <param name="start">The start.</param>
		/// <param name="end">The end.</param>
		/// <returns></returns>
		/// <autogeneratedoc />
		public virtual IReadOnlyList<Block> GetBlocks(DateTime start, DateTime end) => this.GetBlocks(new BlocksInput() { Start = start, End = end, Properties = BlocksProperties.All });

		/// <summary>Gets the blocks.</summary>
		/// <param name="start">The start.</param>
		/// <param name="end">The end.</param>
		/// <param name="filterAccount">The filter account.</param>
		/// <param name="filterIP">The filter ip.</param>
		/// <param name="filterRange">The filter range.</param>
		/// <param name="filterTemporary">The filter temporary.</param>
		/// <returns></returns>
		/// <autogeneratedoc />
		public virtual IReadOnlyList<Block> GetBlocks(DateTime start, DateTime end, Filter filterAccount, Filter filterIP, Filter filterRange, Filter filterTemporary) => this.GetBlocks(new BlocksInput() { Start = start, End = end, FilterAccount = filterAccount, FilterIP = filterIP, FilterRange = filterRange, FilterTemporary = filterTemporary, Properties = BlocksProperties.All });

		/// <summary>Gets the blocks.</summary>
		/// <param name="users">The users.</param>
		/// <returns></returns>
		/// <autogeneratedoc />
		public virtual IReadOnlyList<Block> GetBlocks(IEnumerable<string> users) => this.GetBlocks(new BlocksInput(users) { Properties = BlocksProperties.All });

		/// <summary>Gets the blocks.</summary>
		/// <param name="ip">The ip.</param>
		/// <returns></returns>
		/// <autogeneratedoc />
		public virtual IReadOnlyList<Block> GetBlocks(IPAddress ip) => this.GetBlocks(new BlocksInput(ip) { Properties = BlocksProperties.All });

		/// <summary>Gets multiple messages from MediaWiki space.</summary>
		/// <param name="messages">The messages.</param>
		/// <param name="arguments">Optional arguments to substitute into the messages.</param>
		/// <returns>The text of the message.</returns>
		public virtual IReadOnlyDictionary<string, Message> GetMessages(IEnumerable<string> messages, IEnumerable<string> arguments) => this.GetMessages(new AllMessagesInput
		{
			Messages = messages,
			Arguments = arguments,
		});

		/// <summary>Pages the property names.</summary>
		/// <returns></returns>
		/// <autogeneratedoc />
		public virtual IReadOnlyList<string> PagePropertyNames() => this.AbstractionLayer.PagePropertyNames(new PagePropertyNamesInput());

		/// <summary>Gets the parsed messages.</summary>
		/// <param name="msgs">The MSGS.</param>
		/// <param name="arguments">The arguments.</param>
		/// <param name="title">The title.</param>
		/// <returns></returns>
		/// <autogeneratedoc />
		public virtual IReadOnlyDictionary<string, Message> GetParsedMessages(IEnumerable<string> msgs, IEnumerable<string> arguments, Title title) => this.GetMessages(new AllMessagesInput
		{
			Messages = msgs,
			Arguments = arguments,
			EnableParser = true,
			EnableParserTitle = title?.FullPageName,
		});

		/// <summary>Gets the recent changes.</summary>
		/// <param name="options">The options.</param>
		/// <returns></returns>
		/// <autogeneratedoc />
		public virtual IReadOnlyList<RecentChange> GetRecentChanges(RecentChangesOptions options)
		{
			ThrowNull(options, nameof(options));
			return this.GetRecentChanges(options.ToWallEInput);
		}

		/// <summary>Gets the redirect target.</summary>
		/// <param name="text">The text.</param>
		/// <returns></returns>
		/// <autogeneratedoc />
		public virtual Title GetRedirectTarget(string text)
		{
			if (text != null)
			{
				return null;
			}

			if (this.redirectTargetFinder == null)
			{
				var list = new List<string>();
				var redirects = this.MagicWords.TryGetValue("redirect", out var redirect) ? new HashSet<string>(redirect.Aliases) : new HashSet<string> { "#REDIRECT" };
				foreach (var redirWord in redirects)
				{
					list.Add(Regex.Escape(redirWord));
				}

				// Regex originally taken from WikitextContent.php --> '!^\s*:?\s*\[{2}(.*?)(?:\|.*?)?\]{2}\s*!'
				this.redirectTargetFinder = new Regex("^(" + string.Join("|", list) + @")\s*:?\s*\[{2}(?<target>.*?)(\|.*?)?\]{2}", redirect.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
			}

			var target = this.redirectTargetFinder.Match(text).Groups["target"];
			return target.Success ? new Title(this, target.Value) : null;
		}

		/// <summary>Gets the user information.</summary>
		/// <param name="users">The users.</param>
		/// <returns></returns>
		/// <autogeneratedoc />
		public virtual IReadOnlyList<User> GetUserInformation(IEnumerable<string> users)
		{
			var input = new UsersInput(users)
			{
				Properties = UsersProperties.All
			};
			var result = this.AbstractionLayer.Users(input);
			var retval = new List<User>(result.Count);
			foreach (var item in result)
			{
				var user = new User(this, item);
				retval.Add(user);
			}

			return retval.AsReadOnly();
		}

		/// <summary>Gets the users.</summary>
		/// <param name="onlyActiveUsers">if set to <c>true</c> [only active users].</param>
		/// <param name="onlyUsersWithEdits">if set to <c>true</c> [only users with edits].</param>
		/// <returns></returns>
		/// <autogeneratedoc />
		public virtual IReadOnlyList<string> GetUsers(bool onlyActiveUsers, bool onlyUsersWithEdits) => this.GetUsers(new AllUsersInput { ActiveUsersOnly = onlyActiveUsers, WithEditsOnly = onlyUsersWithEdits });

		/// <summary>Gets the users.</summary>
		/// <param name="onlyActiveUsers">if set to <c>true</c> [only active users].</param>
		/// <param name="onlyUsersWithEdits">if set to <c>true</c> [only users with edits].</param>
		/// <param name="prefix">The prefix.</param>
		/// <returns></returns>
		/// <autogeneratedoc />
		public virtual IReadOnlyList<string> GetUsers(bool onlyActiveUsers, bool onlyUsersWithEdits, string prefix) => this.GetUsers(new AllUsersInput { ActiveUsersOnly = onlyActiveUsers, WithEditsOnly = onlyUsersWithEdits, Prefix = prefix });

		/// <summary>Gets the users.</summary>
		/// <param name="onlyActiveUsers">if set to <c>true</c> [only active users].</param>
		/// <param name="onlyUsersWithEdits">if set to <c>true</c> [only users with edits].</param>
		/// <param name="from">From.</param>
		/// <param name="to">To.</param>
		/// <returns></returns>
		/// <autogeneratedoc />
		public virtual IReadOnlyList<string> GetUsers(bool onlyActiveUsers, bool onlyUsersWithEdits, string from, string to) => this.GetUsers(new AllUsersInput { ActiveUsersOnly = onlyActiveUsers, WithEditsOnly = onlyUsersWithEdits, From = from, To = to });

		/// <summary>Gets the users in groups.</summary>
		/// <param name="onlyActiveUsers">if set to <c>true</c> [only active users].</param>
		/// <param name="onlyUsersWithEdits">if set to <c>true</c> [only users with edits].</param>
		/// <param name="groups">The groups.</param>
		/// <returns></returns>
		/// <autogeneratedoc />
		public virtual IReadOnlyList<string> GetUsersInGroups(bool onlyActiveUsers, bool onlyUsersWithEdits, IEnumerable<string> groups) => this.GetUsers(new AllUsersInput { ActiveUsersOnly = onlyActiveUsers, WithEditsOnly = onlyUsersWithEdits, Groups = groups });

		/// <summary>Gets the users with rights.</summary>
		/// <param name="onlyActiveUsers">if set to <c>true</c> [only active users].</param>
		/// <param name="onlyUsersWithEdits">if set to <c>true</c> [only users with edits].</param>
		/// <param name="rights">The rights.</param>
		/// <returns></returns>
		/// <autogeneratedoc />
		public virtual IReadOnlyList<string> GetUsersWithRights(bool onlyActiveUsers, bool onlyUsersWithEdits, IEnumerable<string> rights) => this.GetUsers(new AllUsersInput { ActiveUsersOnly = onlyActiveUsers, WithEditsOnly = onlyUsersWithEdits, Rights = rights });

		/// <summary>Logs the specified user into the wiki.</summary>
		/// <param name="userName">Name of the user. May be null.</param>
		/// <param name="password">The password.</param>
		/// <param name="domain">The domain.</param>
		/// <exception cref="UnauthorizedAccessException">Thrown if there was an error logging into the wiki (which typically denotes that the user had the wrong password or does not have permission to log in).</exception>
		/// <remarks>Even if you wish to edit anonymously, you <em>must</em> still log in by passing <see langword="null" /> for the <paramref name="userName" /> parameter.</remarks>
		public virtual void Login(string userName, string password, string domain)
		{
			var input = new LoginInput(userName, password) { Domain = domain };
			var result = this.AbstractionLayer.Login(input);
			if (userName != null && result.Result != "Success")
			{
				this.Clear();
				throw new UnauthorizedAccessException(CurrentCulture(LoginFailed, result.Reason));
			}

			this.UserName = result.User;
			this.GetInfo();
		}

		/// <summary>Logs the user out.</summary>
		public virtual void Logout()
		{
			this.Clear();
			this.AbstractionLayer.Logout();
		}

		/// <summary>Patrols the specified rcid.</summary>
		/// <param name="rcid">The rcid.</param>
		/// <returns></returns>
		/// <autogeneratedoc />
		public virtual bool Patrol(long rcid) => this.Patrol(new PatrolInput(rcid));

		/// <summary>Patrols the revision.</summary>
		/// <param name="revid">The revid.</param>
		/// <returns></returns>
		/// <autogeneratedoc />
		public virtual bool PatrolRevision(long revid) => this.Patrol(PatrolInput.FromRevisionId(revid));

		/// <summary>Can be called any time an edit is deliberately ignored to publish the related event.</summary>
		/// <param name="sender">The real sender of the event.</param>
		/// <param name="parameters">The parameters.</param>
		/// <param name="caller">The caller (populated automatically with caller name).</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Justification = "Does not make sense to do so for CallerMemberName.")]
		public virtual void PublishIgnoredEdit(object sender, IReadOnlyDictionary<string, object> parameters, [CallerMemberName] string caller = null) => this.EditIgnored.Invoke(this, new EditIgnoredEventArgs(sender, caller, parameters));

		/// <summary>Publishes the warning.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="warning">The warning.</param>
		/// <autogeneratedoc />
		public virtual void PublishWarning(IMessageSource sender, string warning) => this.WarningOccurred?.Invoke(this, new WarningEventArgs(sender, warning));

		/// <summary>Upload a file to the wiki.</summary>
		/// <param name="fileName">The full path and filename of the file to upload.</param>
		/// <param name="destinationName">The bare name (i.e., do not include "File:") of the file to upload to on the wiki. Set to null to use the filename from the <paramref name="fileName" /> parameter.</param>
		/// <param name="editSummary">The edit summary for the upload.</param>
		/// <param name="pageText">Full page text for the File page. This should include the license, categories, and anything else required. Set to null to allow the wiki to generate the page text (normally just the <paramref name="editSummary" />).</param>
		/// <exception cref="ArgumentException">Path contains an invalid character.</exception>
		public virtual void Upload(string fileName, string destinationName, string editSummary, string pageText)
		{
			if (!this.AllowEditing)
			{
				this.PublishIgnoredEdit(this, new Dictionary<string, object>
				{
					[nameof(fileName)] = fileName,
					[nameof(destinationName)] = destinationName,
					[nameof(editSummary)] = editSummary,
					[nameof(pageText)] = pageText,
				});

				return;
			}

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

				this.AbstractionLayer.Upload(uploadInput);
			}
		}
		#endregion

		#region Protected Virtual Methods

		/// <summary>Downloads a file.</summary>
		/// <param name="input">The input parameters.</param>
		protected virtual void Download(DownloadInput input) => this.AbstractionLayer.Download(input);

		protected virtual IReadOnlyList<Block> GetBlocks(BlocksInput input)
		{
			var result = this.AbstractionLayer.Blocks(input);
			var retval = new List<Block>(result.Count);
			foreach (var item in result)
			{
				retval.Add(new Block(item.User, item.By, item.Reason, item.Timestamp ?? DateTime.MinValue, item.Expiry ?? DateTime.MaxValue, item.Flags, item.Automatic));
			}

			return retval;
		}

		/// <summary>Gets all site information required for proper functioning of the framework.</summary>
		protected virtual void GetInfo()
		{
			var siteInfo = this.AbstractionLayer.SiteInfo(new SiteInfoInput() { Properties = SiteInfoProperties.General | SiteInfoProperties.Namespaces | SiteInfoProperties.NamespaceAliases | SiteInfoProperties.MagicWords });
			this.CaseSensitive = siteInfo.Flags.HasFlag(SiteInfoFlags.CaseSensitive);
			this.Culture = GetCulture(siteInfo.Language);
			this.Name = siteInfo.SiteName;
			this.ServerName = siteInfo.ServerName;
			this.Version = siteInfo.Generator;

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

			var namespaces = new List<Namespace>(siteInfo.Namespaces.Count);
			foreach (var item in siteInfo.Namespaces)
			{
				allAliases.TryGetValue(item.Id, out var aliases);
				namespaces.Add(new Namespace(this, item, aliases));
			}

			this.Namespaces = new NamespaceCollection(namespaces);

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

			this.articlePath = path;

			foreach (var word in siteInfo.MagicWords)
			{
				this.magicWords.Add(word.Name, new MagicWord(word));
			}

			this.DisambiguatorAvailable = this.magicWords.ContainsKey("disambiguation");
		}

		protected virtual IReadOnlyDictionary<string, Message> GetMessages(AllMessagesInput input)
		{
			var result = this.AbstractionLayer.AllMessages(input);
			var retval = new Dictionary<string, Message>(result.Count);
			foreach (var item in result)
			{
				retval.Add(item.Name, new Message(this, item));
			}

			return retval.AsReadOnly();
		}

		protected virtual IReadOnlyList<RecentChange> GetRecentChanges(RecentChangesInput input)
		{
			var result = this.AbstractionLayer.RecentChanges(input);
			var retval = new List<RecentChange>(result.Count);
			foreach (var item in result)
			{
				retval.Add(new RecentChange(this, item));
			}

			return retval;
		}

		protected virtual IReadOnlyList<string> GetUsers(AllUsersInput input)
		{
			ThrowNull(input, nameof(input));
			input.Properties = AllUsersProperties.None;
			var result = this.AbstractionLayer.AllUsers(input);
			var retval = new List<string>(result.Count);
			foreach (var item in result)
			{
				retval.Add(item.Name);
			}

			return retval;
		}

		/// <summary>Loads the disambiguation templates for wikis that don't use Disambiguator.</summary>
		/// <returns>A collection of titles of disambiguation templates.</returns>
		protected virtual ICollection<Title> LoadDisambiguationTemplates()
		{
			this.disambiguationTemplates = new HashSet<Title>();
			var page = new Page(this, MediaWikiNamespaces.MediaWiki, "Disambiguationspage");
			page.Load(PageModules.Default | PageModules.Links);
			if (!page.Missing)
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

		/// <summary>
		/// Patrols the specified input.
		/// </summary>
		/// <param name="input">The input.</param>
		/// <returns></returns>
		/// <autogeneratedoc />
		protected virtual bool Patrol(PatrolInput input)
		{
			if (!this.AllowEditing)
			{
				this.PublishIgnoredEdit(this, new Dictionary<string, object>
				{
					[nameof(input)] = input,
				});

				return true;
			}

			var result = this.AbstractionLayer.Patrol(input);
			return result.Title != null;
		}
		#endregion

		#region Private Methods
		private void Clear()
		{
			this.articlePath = null;
			this.magicWords.Clear();

			this.CaseSensitive = false;
			this.Culture = CultureInfo.CurrentCulture;
			this.DisambiguatorAvailable = false;
			this.Name = null;
			this.Namespaces = null;
			this.ServerName = null;
			this.UserName = null;
			this.Version = null;
		}

		/// <summary>Forwards warning events from the abstraction layer to the site.</summary>
		/// <param name="sender">The sending abstraction layer.</param>
		/// <param name="eventArgs">The event arguments.</param>
		private void Wiki_WarningOccurred(IWikiAbstractionLayer sender, /* Overlapping type names, so must use full name here */ WallE.Design.WarningEventArgs eventArgs)
		{
			var warning = eventArgs.Warning;
			this.PublishWarning(this, "(" + warning.Code + ") " + warning.Info);
		}
		#endregion
	}
}
