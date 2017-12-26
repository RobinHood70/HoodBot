namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.WallE.Base;
	using WikiCommon;
	using static WikiCommon.Globals;

	#region Public Enumerations
	[Flags]
	public enum BlockFlags
	{
		None = 0,
		AllowUserTalk = BlockUserFlags.AllowUserTalk,
		AnonymousOnly = BlockUserFlags.AnonymousOnly,
		AutoBlock = BlockUserFlags.AutoBlock,
		NoCreate = BlockUserFlags.NoCreate,
		NoEmail = BlockUserFlags.NoEmail,
		Reblock = BlockUserFlags.Reblock,
	}
	#endregion

	public class User
	{
		#region Fields
		private static string defaultSubject = null;
		#endregion

		#region Constructors
		public User(Site site, string name)
		{
			ThrowNull(site, nameof(site));
			this.Site = site;
			this.Name = Title.Normalize(name);
			this.Page = new Title(site, MediaWikiNamespaces.User, name);
			this.TalkPage = this.Page.TalkPage;
		}
		#endregion

		#region Public Properties
		public string Name { get; }

		public Title Page { get; }

		public Site Site { get; }

		public Title TalkPage { get; }
		#endregion

		#region Public Methods
		public bool Block(string reason, BlockFlags flags, DateTime expiry)
		{
			var input = new BlockInput(this.Name)
			{
				Expiry = expiry,
				Flags = (BlockUserFlags)flags,
				Reason = reason,
			};
			return this.Block(input);
		}

		public bool Block(string reason, BlockFlags flags, string relativeExpiry)
		{
			var input = new BlockInput(this.Name)
			{
				ExpiryRelative = relativeExpiry,
				Flags = (BlockUserFlags)flags,
				Reason = reason,
			};
			return this.Block(input);
		}

		public string Email(string body, bool ccMe)
		{
			defaultSubject = defaultSubject ?? this.Site.GetParsedMessage("defemailsubject", new[] { this.Site.UserName });
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

		public void NewTalkPageMessage(string header, string message, string editSummary)
		{
			if (!this.Site.AllowEditing)
			{
				return;
			}

			message = message.Trim();
			if (!message.Contains("~~~"))
			{
				// If at least the name wasn't found in the message, then add a normal signature.
				message += " ~~~~";
			}

			var input = new EditInput(this.TalkPage.FullPageName, message)
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
#endregion
	}
}