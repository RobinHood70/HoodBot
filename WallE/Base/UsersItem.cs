#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	#region Public Enumerations
	[Flags]
	public enum UserFlags
	{
		None = 0,
		Emailable = 1,
		Interwiki = 1 << 1,
		Invalid = 1 << 2,
		Missing = 1 << 3
	}
	#endregion

	public class UsersItem : InternalUserItem
	{
		#region Constructors
		internal UsersItem(long userId, string name, UserFlags flags, string? gender, string? token)
			: base(userId, name)
		{
			this.Flags = flags;
			this.Gender = gender;
			this.Token = token;
		}
		#endregion

		#region Public Properties
		public UserFlags Flags { get; }

		public string? Gender { get; }

		public string? Token { get; }
		#endregion
	}
}
