namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Properties;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;

	/// <summary>Represents a user on the wiki. This can include IP users.</summary>
	public class User : Title
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="User"/> class.</summary>
		/// <param name="site">The site the user is from.</param>
		/// <param name="user">The user's name.</param>
		public User(Site site, string user)
			: base(site.NotNull()[MediaWikiNamespaces.User], user.NotNull())
		{
		}

		/// <summary>Initializes a new instance of the <see cref="User"/> class.</summary>
		/// <param name="site">The site the user is from.</param>
		/// <param name="userInfo">The API user information.</param>
		public User(Site site, AllUsersItem userInfo)
			: this(site, userInfo.NotNull().Name)
		{
			this.Info = new UserInfo(this.Site, userInfo);
		}

		/// <summary>Initializes a new instance of the <see cref="User"/> class.</summary>
		/// <param name="title">The base user page.</param>
		/// <param name="userInfo">The API user information.</param>
		public User(Title title, UsersItem userInfo)
			: base(title)
		{
			this.Info = new UserInfo(this.Site, userInfo);
		}
		#endregion

		#region Public Properties

		/// <summary>Gets extended user information, if loaded via <see cref="LoadUserInfo"/>.</summary>
		public UserInfo? Info { get; private set; }

		/// <summary>Gets the user's name.</summary>
		/// <value>The name.</value>
		/// <remarks>This is an alias to PageName for ease-of-use.</remarks>
		public string Name => this.PageName;
		#endregion

		#region Public Static Methods

		/// <summary>Gets the title of the user page, given the name.</summary>
		/// <param name="site">The site the user is from.</param>
		/// <param name="name">The username.</param>
		/// <returns>A title corresponding to the User page.</returns>
		public static Title GetTitle(Site site, string name) => new(TitleFactory.FromValidated(site.NotNull()[MediaWikiNamespaces.User], name.NotNull()));
		#endregion

		#region Public Methods

		/// <summary>Blocks the specified user.</summary>
		/// <param name="reason">The reason for the block.</param>
		/// <param name="flags">The block flags.</param>
		/// <param name="expiry">The date and time the block should expire.</param>
		/// <param name="reblock">if set to <see langword="true"/>, reblocks the user with the new block settings.</param>
		/// <returns>A value indicating the change status of the block.</returns>
		public ChangeStatus Block(string reason, BlockFlags flags, DateTime expiry, bool reblock)
		{
			BlockInput input = new(this.Name)
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
		/// <param name="reblock">if set to <see langword="true"/>, reblocks the user with the new block settings.</param>
		/// <returns>A value indicating the change status of the block.</returns>
		public ChangeStatus Block(string reason, BlockFlags flags, string duration, bool reblock)
		{
			BlockInput input = new(this.Name)
			{
				ExpiryRelative = duration,
				Flags = flags,
				Reason = reason,
				Reblock = reblock,
			};
			return this.Block(input);
		}

		/// <summary>Emails the user.</summary>
		/// <param name="subject">The subject line for the e-mail if not the wiki default.</param>
		/// <param name="body">The e-mail body.</param>
		/// <param name="ccMe">if set to <see langword="true"/>, sends a copy of the e-mail to the user's e-mail account.</param>
		/// <returns>A value indicating the change status of the e-mail along with a copy of the e-mail that was sent.</returns>
		public ChangeValue<string> Email(string subject, string body, bool ccMe)
		{
			subject.ThrowNull();
			body.ThrowNull();
			if (this.Info?.Emailable == false)
			{
				// Don't ask the wiki what the result will be if we already know we can't e-mail them.
				return new ChangeValue<string>(ChangeStatus.Failure, Resources.UserEmailDisabled);
			}

			var disabledResult = string.Empty;
			Dictionary<string, object?> parameters = new(StringComparer.Ordinal)
			{
				[nameof(subject)] = subject,
				[nameof(body)] = body,
				[nameof(ccMe)] = ccMe,
			};

			return this.Site.PublishChange(disabledResult, this, parameters, ChangeFunc);

			ChangeValue<string> ChangeFunc()
			{
				EmailUserInput input = new(this.Name, body) { CCMe = ccMe, Subject = subject };
				var retval = this.Site.AbstractionLayer.EmailUser(input);
				var result = string.Equals(retval.Result, "Success", StringComparison.OrdinalIgnoreCase)
					? ChangeStatus.Success
					: ChangeStatus.Failure;
				if (this.Info is null)
				{
					this.Info = new UserInfo(result == ChangeStatus.Success);
				}

				return new ChangeValue<string>(result, retval.Message ?? retval.Result);
			}
		}

		// CONSIDER: Adding more GetContributions() and GetWatchlist() options.

		/// <summary>Gets the user's entire contribution history.</summary>
		/// <returns>A read-only list with the user's entire contribution history.</returns>
		public IReadOnlyList<Contribution> GetContributions()
		{
			UserContributionsInput input = new(this.Name);
			var result = this.Site.AbstractionLayer.UserContributions(input);
			List<Contribution> retval = new();
			foreach (var item in result)
			{
				retval.Add(new Contribution(this.Site, item));
			}

			return retval;
		}

		/// <summary>Gets the user's contribution history in the specified namespaces.</summary>
		/// <param name="namespaces">The namespaces of the contributions to retrieve.</param>
		/// <returns>A read-only list with the user's contribution history in the specified namespaces.</returns>
		public IReadOnlyList<Contribution> GetContributions(IEnumerable<int> namespaces)
		{
			UserContributionsInput input = new(this.Name) { Namespaces = namespaces };
			var result = this.Site.AbstractionLayer.UserContributions(input);
			List<Contribution> retval = new();
			foreach (var item in result)
			{
				retval.Add(new Contribution(this.Site, item));
			}

			return retval;
		}

		/// <summary>Gets the user's entire contribution history.</summary>
		/// <param name="from">The date and time to start listing contributions from.</param>
		/// <param name="to">The date and time to list contributions to.</param>
		/// <returns>A read-only list with the user's entire contribution history.</returns>
		public IReadOnlyList<Contribution> GetContributions(DateTime? from, DateTime? to)
		{
			UserContributionsInput input = new(this.Name) { Start = from, End = to, SortAscending = (from ?? DateTime.MinValue) < (to ?? DateTime.MaxValue) };
			var result = this.Site.AbstractionLayer.UserContributions(input);
			List<Contribution> retval = new();
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
		public IReadOnlyList<Title> GetWatchlist(string token, IEnumerable<int>? namespaces)
		{
			WatchlistRawInput input = new()
			{
				Owner = this.Name,
				Token = token,
				Namespaces = namespaces
			};
			var result = this.Site.AbstractionLayer.WatchlistRaw(input);
			List<Title> retval = new();
			foreach (var item in result)
			{
				retval.Add(TitleFactory.FromValidated(this.Site, item.FullPageName));
			}

			return retval;
		}

		/// <summary>Loads all user information. This is necessary for any User object not provided by one of the <see cref="Site"/>.LoadUserInformation() methods.</summary>
		/// <remarks>The information loaded includes the following properties: BlockInfo, EditCount, Emailable, Gender, Groups, Registration, and Rights.</remarks>
		public void LoadUserInfo()
		{
			UsersInput input = new(new[] { this.Name })
			{
				Properties = UsersProperties.All
			};
			var result = this.Site.AbstractionLayer.Users(input);
			if (result.Count == 1)
			{
				this.Info = new UserInfo(this.Site, result[0]);
			}
		}

		/// <summary>Creates a new message on the user's talk page.</summary>
		/// <param name="header">The section header.</param>
		/// <param name="msg">The message.</param>
		/// <param name="editSummary">The edit summary.</param>
		/// <returns>A value indicating the change status of posting the new talk page message.</returns>
		/// <exception cref="InvalidOperationException">Thrown when the user's talk page is invalid.</exception>
		public ChangeStatus NewTalkPageMessage(string header, string msg, string editSummary)
		{
			msg = msg.NotNull().Trim();
			if (this.TalkPage is not Title talkPage)
			{
				throw new InvalidOperationException(Resources.TitleInvalid);
			}

			if (!msg.Contains("~~~", StringComparison.Ordinal) && !msg.Contains(":" + this.Name, StringComparison.Ordinal))
			{
				// If at least the name wasn't found in the message, then add a normal signature.
				msg += " ~~~~";
			}

			Dictionary<string, object?> parameters = new(StringComparer.Ordinal)
			{
				[nameof(header)] = header,
				[nameof(msg)] = msg,
				[nameof(editSummary)] = editSummary,
			};

			return this.Site.PublishChange(this, parameters, ChangeFunc);

			ChangeStatus ChangeFunc()
			{
				EditInput input = new(talkPage.FullPageName, msg)
				{
					Bot = true,
					Minor = Tristate.False,
					Recreate = true,
					Section = -1,
					SectionTitle = header,
					Summary = editSummary,
				};

				var retval = this.Site.AbstractionLayer.Edit(input);

				return string.Equals(retval.Result, "Success", StringComparison.OrdinalIgnoreCase)
					? ChangeStatus.Success
					: ChangeStatus.Failure;
			}
		}

		/// <summary>Unblocks the user for the specified reason.</summary>
		/// <param name="reason">The unblock reason.</param>
		/// <returns>A value indicating the change status of the unblock.</returns>
		public ChangeStatus Unblock(string reason)
		{
			Dictionary<string, object?> parameters = new(StringComparer.Ordinal)
			{
				[nameof(reason)] = reason
			};

			return this.Site.PublishChange(this, parameters, ChangeFunc);

			ChangeStatus ChangeFunc()
			{
				UnblockInput input = new(this.Name) { Reason = reason };
				var retval = this.Site.AbstractionLayer.Unblock(input);
				return retval.Id == 0
					? ChangeStatus.Failure
					: ChangeStatus.Success;
			}
		}
		#endregion

		#region Private Methods
		private ChangeStatus Block(BlockInput input)
		{
			Dictionary<string, object?> parameters = new(StringComparer.Ordinal)
			{
				[nameof(input.User)] = input.User,
				[nameof(input.Reason)] = input.Reason,
				[nameof(input.Expiry)] = input.Expiry,
				[nameof(input.ExpiryRelative)] = input.ExpiryRelative,
				[nameof(input.Flags)] = input.Flags,
				[nameof(input.Reblock)] = input.Reblock,
			};

			return this.Site.PublishChange(this, parameters, ChangeFunc);

			ChangeStatus ChangeFunc()
			{
				var retval = this.Site.AbstractionLayer.Block(input);
				return retval.Id == 0
					? ChangeStatus.Failure
					: ChangeStatus.Success;
			}
		}
		#endregion
	}
}