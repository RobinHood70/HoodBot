namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;
	using static WikiCommon.Globals;

	/// <summary>Represents a user on the wiki. This can include IP users.</summary>
	public class User
	{
		#region Static Fields
		private static string defaultSubject = null;
		private static string emailDisabled = null;
		#endregion

		#region Fields
		private bool loaded = false;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="User"/> class.</summary>
		/// <param name="site">The site the user is from.</param>
		/// <param name="name">The name of the user.</param>
		public User(Site site, string name)
		{
			ThrowNull(site, nameof(site));
			ThrowNull(name, nameof(name));
			this.Site = site;
			this.Name = TitleParts.DecodeAndNormalize(name);
			this.Page = new Title(site.Namespaces[MediaWikiNamespaces.User], name);
		}

		/// <summary>Initializes a new instance of the <see cref="User"/> class.</summary>
		/// <param name="site">The site the user is from.</param>
		/// <param name="user">The WallE <see cref="UsersInput"/> to populate the data from.</param>
		protected internal User(Site site, UsersItem user)
			: this(site, user?.Name)
		{
			ThrowNull(user, nameof(user));
			this.Populate(user);
		}
		#endregion

		#region Public Properties

		/// <summary>Gets information about any active blocks on the user.</summary>
		/// <value>The block information.</value>
		public Block BlockInfo { get; private set; }

		/// <summary>Gets the user's edit count.</summary>
		/// <value>The user's edit count.</value>
		public long EditCount { get; private set; }

		/// <summary>Gets a value indicating whether this <see cref="User"/> can be e-mailed.</summary>
		/// <value><c>true</c> if the user is emailable; otherwise, <c>false</c>.</value>
		public bool Emailable { get; private set; }

		/// <summary>Gets the user's gender.</summary>
		/// <value>The user's gender.</value>
		public string Gender { get; private set; }

		/// <summary>Gets the groups the user belongs to.</summary>
		/// <value>The groups the user belongs to.</value>
		public IReadOnlyList<string> Groups { get; private set; }

		/// <summary>Gets the user's name.</summary>
		/// <value>The name.</value>
		public string Name { get; }

		/// <summary>Gets a <see cref="Title"/> representing the the user page.</summary>
		/// <value>The user page.</value>
		public Title Page { get; }

		/// <summary>Gets the date and time the user account was created.</summary>
		/// <value>The user's registration date.</value>
		public DateTime Registration { get; private set; }

		/// <summary>Gets the user's rights.</summary>
		/// <value>The user's rights.</value>
		public IReadOnlyList<string> Rights { get; private set; }

		/// <summary>Gets the site the user is from.</summary>
		/// <value>The site the user is from.</value>
		public Site Site { get; }

		/// <summary>Gets the user's talk page.</summary>
		/// <value>The user's talk page.</value>
		public Title TalkPage => this.Page.TalkPage;
		#endregion

		#region Public Methods

		/// <summary>Blocks the specified user.</summary>
		/// <param name="reason">The reason for the block.</param>
		/// <param name="flags">The block flags.</param>
		/// <param name="expiry">The date and time the block should expire.</param>
		/// <param name="reblock">if set to <c>true</c>, reblocks the user with the new block settings.</param>
		/// <returns><c>true</c> if the block was successful.</returns>
		public bool Block(string reason, BlockFlags flags, DateTime expiry, bool reblock)
		{
			var input = new BlockInput(this.Name)
			{
				Expiry = expiry,
				Flags = flags,
				Reason = reason,
				Reblock = reblock,
			};
			return this.Block(input);
		}

		/// <summary>Blocks the specified user.</summary>
		/// <param name="reason">The reason for the block.</param>
		/// <param name="flags">The block flags.</param>
		/// <param name="duration">The duration of the block (e.g., "2 weeks").</param>
		/// <param name="reblock">if set to <c>true</c>, reblocks the user with the new block settings.</param>
		/// <returns><c>true</c> if the block was successful.</returns>
		public bool Block(string reason, BlockFlags flags, string duration, bool reblock)
		{
			var input = new BlockInput(this.Name)
			{
				ExpiryRelative = duration,
				Flags = flags,
				Reason = reason,
				Reblock = reblock,
			};
			return this.Block(input);
		}

		/// <summary>Emails the user.</summary>
		/// <param name="body">The e-mail body.</param>
		/// <param name="ccMe">if set to <c>true</c>, sends a copy of the e-mail to the bot's e-mail account.</param>
		/// <returns>A warning message if there was a problem with sending the e-mail; otherwise null.</returns>
		/// <remarks>The subject of the e-mail will be the wiki default.</remarks>
		public string Email(string body, bool ccMe)
		{
			defaultSubject = defaultSubject ?? this.Site.LoadParsedMessage("defemailsubject").Replace("$1", this.Site.UserName);
			return this.Email(defaultSubject, body, ccMe);
		}

		/// <summary>Emails the user.</summary>
		/// <param name="subject">The subject line for the e-mail if not the wiki default.</param>
		/// <param name="body">The e-mail body.</param>
		/// <param name="ccMe">if set to <c>true</c>, sends a copy of the e-mail to the bot's e-mail account.</param>
		/// <returns>"Success" if the e-mail was sent successfully or a warning message if there was a problem.</returns>
		public string Email(string subject, string body, bool ccMe)
		{
			if (this.loaded && !this.Emailable)
			{
				// Don't ask the wiki what the result will be if we already know we can't e-mail them. Load the e-mail disabled message if we don't already have it and just return that.
				emailDisabled = emailDisabled ?? this.Site.LoadParsedMessage("usermaildisabled");
				return emailDisabled;
			}

			var input = new EmailUserInput(this.Name, body)
			{
				CCMe = ccMe,
				Subject = subject
			};
			var result = this.Site.AbstractionLayer.EmailUser(input);
			return result.Message ?? result.Result;
		}

		// CONSIDER: Adding more GetContributions() and GetWatchlist() options.

		/// <summary>Gets the user's entire contribution history.</summary>
		/// <returns>A read-only list with the user's entire contribution history.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Method performs a time-consuming operation.")]
		public IReadOnlyList<Contribution> GetContributions()
		{
			var input = new UserContributionsInput(new[] { this.Name });
			var result = this.Site.AbstractionLayer.UserContributions(input);
			var retval = new List<Contribution>();
			foreach (var item in result)
			{
				retval.Add(new Contribution(this.Site, item));
			}

			return retval;
		}

		/// <summary>Gets the user's contribution history in the specified namespaces.</summary>
		/// <param name="namespaces">The namespaces of the contributions to retrieve.</param>
		/// <returns>A read-only list with the user's contribution history in the specified namespaces.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Method performs a time-consuming operation.")]
		public IReadOnlyList<Contribution> GetContributions(IEnumerable<int> namespaces)
		{
			var input = new UserContributionsInput(new[] { this.Name }) { Namespaces = namespaces };
			var result = this.Site.AbstractionLayer.UserContributions(input);
			var retval = new List<Contribution>();
			foreach (var item in result)
			{
				retval.Add(new Contribution(this.Site, item));
			}

			return retval;
		}

		/// <summary>Gets the user's entire watchlist.</summary>
		/// <param name="token">The user's watchlist token. This must be provided by the user.</param>
		/// <returns>A read-only list of <see cref="Title"/>s in the user's watchlist.</returns>
		public IReadOnlyList<Title> GetWatchlist(string token) => this.GetWatchlist(token, null);

		/// <summary>Gets the user's watchlist.</summary>
		/// <param name="token">The user's watchlist token. This must be provided by the user.</param>
		/// <param name="namespaces">The namespaces of the contributions to retrieve.</param>
		/// <returns>A read-only list of <see cref="Title"/>s in the user's watchlist.</returns>
		public IReadOnlyList<Title> GetWatchlist(string token, IEnumerable<int> namespaces)
		{
			var input = new WatchlistRawInput
			{
				Owner = this.Name,
				Token = token,
				Namespaces = namespaces
			};
			var result = this.Site.AbstractionLayer.WatchlistRaw(input);
			var retval = new List<Title>();
			foreach (var item in result)
			{
				retval.Add(new Title(this.Site, item.Title));
			}

			return retval;
		}

		/// <summary>Loads all user information. This is necessary for any User object not provided by one of the <see cref="Site"/>.LoadUserInformation() methods.</summary>
		/// <remarks>The information loaded includes the following properties: BlockInfo, EditCount, Emailable, Gender, Groups, Registration, and Rights.</remarks>
		public void Load()
		{
			var input = new UsersInput(new[] { this.Name })
			{
				Properties = UsersProperties.All
			};
			var result = this.Site.AbstractionLayer.Users(input);
			var user = result.First();
			this.Populate(user);
		}

		/// <summary>Creates a new message on the user's talk page.</summary>
		/// <param name="header">The section header.</param>
		/// <param name="msg">The message.</param>
		/// <param name="editSummary">The edit summary.</param>
		public void NewTalkPageMessage(string header, string msg, string editSummary)
		{
			if (!this.Site.AllowEditing)
			{
				this.Site.PublishIgnoredEdit(this, new Dictionary<string, object>
				{
					[nameof(header)] = header,
					[nameof(msg)] = msg,
					[nameof(editSummary)] = editSummary,
				});

				return;
			}

			ThrowNull(msg, nameof(msg));
			msg = msg.Trim();
			if (!msg.Contains("~~~"))
			{
				// If at least the name wasn't found in the message, then add a normal signature.
				msg += " ~~~~";
			}

			var input = new EditInput(this.TalkPage.FullPageName, msg)
			{
				Bot = true,
				Minor = Tristate.False,
				Recreate = true,
				Section = -1,
				SectionTitle = header,
				Summary = editSummary,
			};
			this.Site.AbstractionLayer.Edit(input);
		}

		/// <summary>Unblocks the user for the specified reason.</summary>
		/// <param name="reason">The unblock reason.</param>
		/// <returns><c>true</c> if the user was successfully unblocked.</returns>
		public bool Unblock(string reason)
		{
			if (!this.Site.AllowEditing)
			{
				this.Site.PublishIgnoredEdit(this, new Dictionary<string, object>
				{
					[nameof(reason)] = reason,
				});

				return true;
			}

			var input = new UnblockInput(this.Name)
			{
				Reason = reason
			};
			var result = this.Site.AbstractionLayer.Unblock(input);

			return result.Id != 0;
		}
		#endregion

		#region Private Methods
		private bool Block(BlockInput input)
		{
			var result = this.Site.AbstractionLayer.Block(input);
			return result.Id != 0;
		}

		private void Populate(UsersItem user)
		{
			this.BlockInfo = new Block(user.Name, user.BlockedBy, user.BlockReason, user.BlockTimestamp ?? DateTime.MinValue, user.BlockExpiry ?? DateTime.MaxValue, BlockFlags.None, false);
			this.EditCount = user.EditCount;
			this.Emailable = user.Flags.HasFlag(UserFlags.Emailable);
			this.Gender = user.Gender;
			this.Groups = user.Groups;
			this.Registration = user.Registration ?? DateTime.MinValue;
			this.Rights = user.Rights;
			this.loaded = true;
		}
		#endregion
	}
}