namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;

	/// <summary>Stores extended user information.</summary>
	public class UserInfo
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="UserInfo"/> class.</summary>
		/// <param name="emailable">Whether the user is emailable or not.</param>
		public UserInfo(bool emailable)
		{
			this.Emailable = emailable;
		}

		/// <summary>Initializes a new instance of the <see cref="UserInfo"/> class.</summary>
		/// <param name="site">The site the user is on.</param>
		/// <param name="userItem">The API user information.</param>
		public UserInfo(Site site, AllUsersItem userItem)
		{
			ArgumentNullException.ThrowIfNull(site);
			ArgumentNullException.ThrowIfNull(userItem);
			User user = new(site, userItem.Name);
			var by = userItem.BlockedBy == null ? null : new User(site, userItem.BlockedBy);
			var timestamp = userItem.BlockTimestamp ?? DateTime.MinValue;
			this.BlockInfo = new Block(user, by, userItem.BlockReason, timestamp, userItem.BlockExpiry ?? DateTime.MaxValue, BlockFlags.None, false);
			this.EditCount = userItem.EditCount;
			List<string> groups = [];
			if (userItem.Groups != null)
			{
				groups.AddRange(userItem.Groups);
			}

			if (userItem.ImplicitGroups != null)
			{
				groups.AddRange(userItem.ImplicitGroups);
			}

			this.Groups = groups;
			this.Registration = userItem.Registration ?? DateTime.MinValue;
			this.Rights = userItem.Rights ?? [];
		}

		/// <summary>Initializes a new instance of the <see cref="UserInfo"/> class.</summary>
		/// <param name="site">The site the user is on.</param>
		/// <param name="userItem">The API user information.</param>
		public UserInfo(Site site, UsersItem userItem)
		{
			ArgumentNullException.ThrowIfNull(site);
			ArgumentNullException.ThrowIfNull(userItem);
			User user = new(site, userItem.Name);
			var by = userItem.BlockedBy == null ? null : new User(site, userItem.BlockedBy);
			var timestamp = userItem.BlockTimestamp ?? DateTime.MinValue;
			this.BlockInfo = new Block(user, by, userItem.BlockReason, timestamp, userItem.BlockExpiry ?? DateTime.MaxValue, BlockFlags.None, false);
			this.EditCount = userItem.EditCount;
			this.Emailable = userItem.Flags.HasAnyFlag(UserFlags.Emailable);
			this.Gender = userItem.Gender;
			List<string> groups = [];
			if (userItem.Groups != null)
			{
				groups.AddRange(userItem.Groups);
			}

			if (userItem.ImplicitGroups != null)
			{
				groups.AddRange(userItem.ImplicitGroups);
			}

			this.Groups = groups;
			this.Registration = userItem.Registration ?? DateTime.MinValue;
			this.Rights = userItem.Rights ?? [];
		}
		#endregion

		#region Public Properties

		/// <summary>Gets information about any active blocks on the user.</summary>
		/// <value>The block information.</value>
		public Block? BlockInfo { get; }

		/// <summary>Gets the user's edit count.</summary>
		/// <value>The user's edit count.</value>
		public long EditCount { get; }

		/// <summary>Gets a value indicating whether this <see cref="User"/> can be e-mailed.</summary>
		/// <value><see langword="true"/> if the user is emailable; otherwise, <see langword="false"/>.</value>
		public bool? Emailable { get; private set; }

		/// <summary>Gets the user's gender.</summary>
		/// <value>The user's gender.</value>
		public string? Gender { get; }

		/// <summary>Gets the groups the user belongs to.</summary>
		/// <value>The groups the user belongs to.</value>
		public IReadOnlyList<string>? Groups { get; }

		/// <summary>Gets the date and time the user account was created.</summary>
		/// <value>The user's registration date.</value>
		public DateTime Registration { get; }

		/// <summary>Gets the user's rights.</summary>
		/// <value>The user's rights.</value>
		public IReadOnlyList<string>? Rights { get; }
		#endregion
	}
}