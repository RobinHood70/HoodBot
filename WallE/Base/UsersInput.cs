#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;
	using static Globals;

	#region Public Enumerations
	[Flags]
	public enum UsersProperties
	{
		None = 0,
		BlockInfo = 1,
		Groups = 1 << 1,
		ImplicitGroups = 1 << 2,
		Rights = 1 << 3,
		EditCount = 1 << 4,
		Registration = 1 << 5,
		Emailable = 1 << 6,
		Gender = 1 << 7,
		All = BlockInfo | Groups | ImplicitGroups | Rights | EditCount | Registration | Emailable | Gender
	}
	#endregion

	public class UsersInput
	{
		#region Constructors
		public UsersInput(IEnumerable<string> users) => this.Users = users;

		public UsersInput(IEnumerable<long> userIds)
		{
			ThrowNullCollection(userIds, nameof(userIds));
			this.UserIds = userIds;
		}
		#endregion

		#region Public Properties
		public bool GetRightsToken { get; set; }

		public UsersProperties Properties { get; set; }

		public IEnumerable<long> UserIds { get; }

		public IEnumerable<string> Users { get; }
		#endregion
	}
}
