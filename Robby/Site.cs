namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Net;
	using System.Text.RegularExpressions;
	using Design;
	using Pages;
	using WallE.Base;
	using WikiCommon;
	using static Properties.Resources;
	using static WikiCommon.Globals;

	public class Site : IMessageSource
	{
		#region Fields
		private string articlePath;
		private HashSet<Title> disambiguationTemplates = null;
		private Dictionary<string, MagicWord> magicWords = new Dictionary<string, MagicWord>();
		private Regex redirectTargetFinder;
		#endregion

		#region Constructors
		public Site(IWikiAbstractionLayer wiki)
		{
			this.DefaultLoadOptions = new PageLoadOptions(PageModules.Simple);
			this.PageBuilder = new PageBuilder();
			this.AbstractionLayer = wiki;
			this.AbstractionLayer.WarningOccurred += this.Wiki_WarningOccurred;
		}
		#endregion

		#region Events
		public event StrongEventHandler<Site, WarningEventArgs> WarningOccurred;
		#endregion

		#region Public Properties

		public IWikiAbstractionLayer AbstractionLayer { get; }

		/// <summary>Gets or sets a value indicating whether methods that would alter the wiki should be allowed.</summary>
		/// <remarks>If set to false, most methods will silently fail, indicating success whenever possible.</remarks>
		public bool AllowEditing { get; set; } = true;

		public bool CaseSensitive { get; private set; }

		public PageLoadOptions DefaultLoadOptions { get; set; }

		public ICollection<Title> DisambiguationTemplates
		{
			get
			{
				if (this.disambiguationTemplates == null)
				{
					this.LoadDisambiguationTemplates();
				}

				return this.disambiguationTemplates;
			}
		}

		public bool DisambiguatorAvailable { get; private set; }

		public IReadOnlyDictionary<string, MagicWord> MagicWords => this.magicWords;

		public string Name { get; private set; }

		public NamespaceCollection Namespaces { get; private set; }

		public PageBuilderBase PageBuilder { get; set; }

		public string ServerName { get; private set; }

		public string UserName { get; private set; }

		public string Version { get; private set; }
		#endregion

		#region Public Static Methods
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "RobinHood70.Robby.Globals.CurrentCulture(System.String,System.Object[])", Justification = "I'm allowing English only here because it's only intended for debugging.")]
		public static void DebugWarningEventHandler(Site sender, WarningEventArgs e)
		{
			ThrowNull(sender, nameof(sender));
			ThrowNull(e, nameof(e));
			Debug.WriteLine(CurrentCulture(Warning, e.Sender.GetType(), e.Warning), sender.ToString());
		}
		#endregion

		#region Public Methods
		public bool ClearMessage() => this.AbstractionLayer.ClearHasMessage();

		public virtual void DownloadFile(string resource, string fileName) => this.AbstractionLayer.Download(new DownloadInput(resource, fileName));

		public void DownloadFile(Uri uri, string fileName) => this.DownloadFile(uri?.OriginalString, fileName);

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

		public IReadOnlyList<Block> GetBlocks() => this.GetBlocks(new BlocksInput() { Properties = BlocksProperties.All });

		public IReadOnlyList<Block> GetBlocks(Filter filterAccount, Filter filterIP, Filter filterRange, Filter filterTemporary) => this.GetBlocks(new BlocksInput() { FilterAccount = filterAccount, FilterIP = filterIP, FilterRange = filterRange, FilterTemporary = filterTemporary, Properties = BlocksProperties.All });

		public IReadOnlyList<Block> GetBlocks(DateTime start, DateTime end) => this.GetBlocks(new BlocksInput() { Start = start, End = end, Properties = BlocksProperties.All });

		public IReadOnlyList<Block> GetBlocks(DateTime start, DateTime end, Filter filterAccount, Filter filterIP, Filter filterRange, Filter filterTemporary) => this.GetBlocks(new BlocksInput() { Start = start, End = end, FilterAccount = filterAccount, FilterIP = filterIP, FilterRange = filterRange, FilterTemporary = filterTemporary, Properties = BlocksProperties.All });

		public IReadOnlyList<Block> GetBlocks(IEnumerable<string> users) => this.GetBlocks(new BlocksInput(users) { Properties = BlocksProperties.All });

		public IReadOnlyList<Block> GetBlocks(IPAddress ip) => this.GetBlocks(new BlocksInput(ip) { Properties = BlocksProperties.All });

		public string GetMessage(string message, IEnumerable<string> arguments)
		{
			var messages = this.GetMessages(new[] { message }, arguments);
			return messages[message].Text;
		}

		public IReadOnlyDictionary<string, Message> GetMessages(IEnumerable<string> messages, IEnumerable<string> arguments) => this.GetMessages(new AllMessagesInput
		{
			Messages = messages,
			Arguments = arguments,
		});

		public IReadOnlyList<string> PagePropertyNames => this.AbstractionLayer.PagePropertyNames(new PagePropertyNamesInput());

		public string GetParsedMessage(string message, IEnumerable<string> arguments) => this.GetParsedMessage(message, arguments, null);

		public string GetParsedMessage(string message, IEnumerable<string> arguments, Title title)
		{
			var messages = this.GetParsedMessages(new[] { message }, arguments, title);
			return messages[message].Text;
		}

		public IReadOnlyDictionary<string, Message> GetParsedMessages(IEnumerable<string> messages, IEnumerable<string> arguments) => this.GetParsedMessages(messages, arguments, null);

		public IReadOnlyDictionary<string, Message> GetParsedMessages(IEnumerable<string> messages, IEnumerable<string> arguments, Title title) => this.GetMessages(new AllMessagesInput
		{
			Messages = messages,
			Arguments = arguments,
			EnableParser = true,
			EnableParserTitle = title?.FullPageName,
		});

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

		public IReadOnlyList<User> GetUserInformation(params string[] users) => this.GetUserInformation(users as IEnumerable<string>);

		public IReadOnlyList<User> GetUserInformation(IEnumerable<string> users)
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

		public IReadOnlyList<string> GetUsers(bool onlyActiveUsers, bool onlyUsersWithEdits) => this.GetUsers(new AllUsersInput { ActiveUsersOnly = onlyActiveUsers, WithEditsOnly = onlyUsersWithEdits });

		public IReadOnlyList<string> GetUsers(bool onlyActiveUsers, bool onlyUsersWithEdits, string prefix) => this.GetUsers(new AllUsersInput { ActiveUsersOnly = onlyActiveUsers, WithEditsOnly = onlyUsersWithEdits , Prefix = prefix });

		public IReadOnlyList<string> GetUsers(bool onlyActiveUsers, bool onlyUsersWithEdits, string from, string to) => this.GetUsers(new AllUsersInput { ActiveUsersOnly = onlyActiveUsers, WithEditsOnly = onlyUsersWithEdits, From = from, To = to });

		public IReadOnlyList<string> GetUsersInGroups(bool onlyActiveUsers, bool onlyUsersWithEdits, params string[] groups) => this.GetUsersInGroups(onlyActiveUsers, onlyUsersWithEdits, groups as IEnumerable<string>);

		public IReadOnlyList<string> GetUsersInGroups(bool onlyActiveUsers, bool onlyUsersWithEdits, IEnumerable<string> groups) => this.GetUsers(new AllUsersInput { ActiveUsersOnly = onlyActiveUsers, WithEditsOnly = onlyUsersWithEdits, Groups = groups });

		public IReadOnlyList<string> GetUsersWithRights(bool onlyActiveUsers, bool onlyUsersWithEdits, params string[] rights) => this.GetUsersWithRights(onlyActiveUsers, onlyUsersWithEdits, rights as IEnumerable<string>);

		public IReadOnlyList<string> GetUsersWithRights(bool onlyActiveUsers, bool onlyUsersWithEdits, IEnumerable<string> rights) => this.GetUsers(new AllUsersInput { ActiveUsersOnly = onlyActiveUsers, WithEditsOnly = onlyUsersWithEdits, Rights = rights });

		public void Login(string userName, string password) => this.Login(userName, password, null);

		public virtual void Login(string userName, string password, string domain)
		{
			var input = new LoginInput(userName, password) { Domain = domain };
			var result = this.AbstractionLayer.Login(input);
			if (userName != null && result.Result != "Success")
			{
				throw new UnauthorizedAccessException(CurrentCulture(LoginFailed, result.Reason));
			}

			this.UserName = result.User;
			this.GetInfo();
		}

		public virtual void Logout() => this.AbstractionLayer.Logout();

		public virtual void PublishWarning(IMessageSource sender, string warning) => this.WarningOccurred?.Invoke(this, new WarningEventArgs(sender, warning));

		/// <summary>Upload a file to the wiki.</summary>
		/// <param name="fileName">The full path and filename of the file to upload.</param>
		/// <param name="editSummary">The edit summary for the upload.</param>
		/// <remarks>The destination filename will be the same as the local filename.</remarks>
		/// <exception cref="ArgumentException">Path contains an invalid character.</exception>
		public void Upload(string fileName, string editSummary) => this.Upload(fileName, null, editSummary, null);

		/// <summary>Upload a file to the wiki.</summary>
		/// <param name="fileName">The full path and filename of the file to upload.</param>
		/// <param name="destinationName">The bare name (i.e., do not include "File:") of the file to upload to on the wiki. Set to null to use the filename from the <paramref name="fileName"/> parameter.</param>
		/// <param name="editSummary">The edit summary for the upload.</param>
		/// <remarks>The destination filename will be the same as the local filename.</remarks>
		/// <exception cref="ArgumentException">Path contains an invalid character.</exception>
		public void Upload(string fileName, string destinationName, string editSummary) => this.Upload(fileName, destinationName, editSummary, null);

		/// <summary>Upload a file to the wiki.</summary>
		/// <param name="fileName">The full path and filename of the file to upload.</param>
		/// <param name="destinationName">The bare name (i.e., do not include "File:") of the file to upload to on the wiki. Set to null to use the filename from the <paramref name="fileName"/> parameter.</param>
		/// <param name="editSummary">The edit summary for the upload.</param>
		/// <param name="pageText">Full page text for the File page. This should include the license, categories, and anything else required. Set to null to allow the wiki to generate the page text (normally just the <paramref name="editSummary"/>).</param>
		/// <exception cref="ArgumentException">Path contains an invalid character.</exception>
		public virtual void Upload(string fileName, string destinationName, string editSummary, string pageText)
		{
			if (!this.AllowEditing)
			{
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

		#region Protected Methods

		/// <summary>Gets all site information required for proper functioning of the framework.</summary>
		protected virtual void GetInfo()
		{
			var siteInfo = this.AbstractionLayer.SiteInfo(new SiteInfoInput() { Properties = SiteInfoProperties.General | SiteInfoProperties.Namespaces | SiteInfoProperties.NamespaceAliases | SiteInfoProperties.MagicWords });
			this.CaseSensitive = siteInfo.Flags.HasFlag(SiteInfoFlags.CaseSensitive);
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
				namespaces.Add(new Namespace(item, aliases));
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

		protected virtual void LoadDisambiguationTemplates()
		{
			string text;
			var page = new Page(this, MediaWikiNamespaces.MediaWiki, "Disambiguationspage");
			page.Load(PageModules.Simple | PageModules.Links);
			if (page.Missing)
			{
				return;
			}

			text = page.Text.Trim();
			this.disambiguationTemplates = new HashSet<Title>(new WikiTitleEqualityComparer());
			if (page.Links.Count == 0)
			{
				this.disambiguationTemplates.Add(new Title(this, text));
			}
			else
			{
				this.disambiguationTemplates.UnionWith(page.Links);
			}
		}
		#endregion

		#region Private Methods

		private IReadOnlyList<Block> GetBlocks(BlocksInput input)
		{
			var result = this.AbstractionLayer.Blocks(input);
			var retval = new List<Block>(result.Count);
			foreach (var item in result)
			{
				retval.Add(new Block(item.User, item.By, item.Reason, item.Timestamp ?? DateTime.MinValue, item.Expiry ?? DateTime.MaxValue, (BlockFlags)item.Flags, item.Automatic));
			}

			return retval;
		}

		private IReadOnlyDictionary<string, Message> GetMessages(AllMessagesInput input)
		{
			var result = this.AbstractionLayer.AllMessages(input);
			var retval = new Dictionary<string, Message>(result.Count);
			foreach (var item in result)
			{
				var message = new Message(this, MediaWikiNamespaces.MediaWiki, item.Name);
				message.Populate(item);
				retval.Add(item.Name, message);
			}

			return retval.AsReadOnly();
		}

		private IReadOnlyList<string> GetUsers(AllUsersInput input)
		{
			input.Properties = AllUsersProperties.None;
			var result = this.AbstractionLayer.AllUsers(input);
			var retval = new List<string>(result.Count);
			foreach (var item in result)
			{
				retval.Add(item.Name);
			}

			return retval;
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
