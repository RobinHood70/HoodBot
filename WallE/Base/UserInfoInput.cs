#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	#region Public Enumerations
	[Flags]
	public enum UserInfoProperties
	{
		None = 0,
		BlockInfo = 1,
		HasMsg = 1 << 1,
		Groups = 1 << 2,
		ImplicitGroups = 1 << 3,
		Rights = 1 << 4,
		ChangeableGroups = 1 << 5,
		Options = 1 << 6,
		PreferencesToken = 1 << 7,
		EditCount = 1 << 8,
		RateLimits = 1 << 9,
		RealName = 1 << 10,
		Email = 1 << 11,
		AcceptLang = 1 << 12,
		RegistrationDate = 1 << 13,
		UnreadCount = 1 << 14,
		All = BlockInfo | HasMsg | Groups | ImplicitGroups | Rights | ChangeableGroups | Options | PreferencesToken | EditCount | RateLimits | RealName | Email | AcceptLang | RegistrationDate | UnreadCount
	}
	#endregion

	public class UserInfoInput
	{
		#region Public Properties
		public UserInfoProperties Properties { get; set; }
		#endregion
	}
}
