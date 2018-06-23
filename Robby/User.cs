namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using Design;
	using WallE.Base;
	using WikiCommon;
	using static WikiCommon.Globals;

	public class User
	{
		#region Fields
		private static string defaultSubject = null;
		#endregion

		#region Constructors
		public User(Site site, string name)
		{
			ThrowNull(site, nameof(site));
			ThrowNull(name, nameof(name));
			this.Site = site;
			this.Name = TitleParts.DecodeAndNormalize(name);
			this.Page = new Title(site.Namespaces[MediaWikiNamespaces.User], name);
			this.TalkPage = this.Page.TalkPage;
		}

		protected internal User(Site site, UsersItem user)
			: this(site, user?.Name)
		{
			ThrowNull(user, nameof(user));
			this.Populate(user);
		}
		#endregion

		#region Public Properties
		public Block BlockInfo { get; private set; }

		public long EditCount { get; private set; }

		public bool Emailable { get; private set; }

		public string Gender { get; private set; }

		public IReadOnlyList<string> Groups { get; private set; }

		public string Name { get; }

		public Title Page { get; }

		public DateTime Registration { get; private set; }

		public IReadOnlyList<string> Rights { get; private set; }

		public Site Site { get; }

		public Title TalkPage { get; }
		#endregion

		#region Public Methods
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

		public bool Block(string reason, BlockFlags flags, string relativeExpiry, bool reblock)
		{
			var input = new BlockInput(this.Name)
			{
				ExpiryRelative = relativeExpiry,
				Flags = flags,
				Reason = reason,
				Reblock = reblock,
			};
			return this.Block(input);
		}

		public string Email(string body, bool ccMe)
		{
			defaultSubject = defaultSubject ?? this.Site.LoadParsedMessage("defemailsubject", new[] { this.Site.UserName });
			return this.Email(defaultSubject, body, ccMe);
		}

		public string Email(string subject, string body, bool ccMe)
		{
			var input = new EmailUserInput(this.Name, body)
			{
				CCMe = ccMe,
				Subject = subject
			};
			var result = this.Site.AbstractionLayer.EmailUser(input);
			return result.Result;
		}

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
		}
		#endregion
	}
}