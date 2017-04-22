#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	public class LoginResult
	{
		#region Public Static Properties
		public static LoginResult EditingAnonymously => new LoginResult()
		{
			UserId = 0,
			User = null,
			Result = "Success",
			Reason = "Editing anonymously",
		};
		#endregion

		#region Public Properties
		public string Reason { get; set; }

		public string Result { get; set; }

		public string Token { get; set; }

		public long UserId { get; set; }

		public string User { get; set; }

		public TimeSpan WaitTime { get; set; }
		#endregion

		#region Public Static Methods
		public static LoginResult AlreadyLoggedIn(long userId, string userName) => new LoginResult()
		{
			UserId = userId,
			User = userName,
			Result = "Success",
			Reason = "Already logged in",
		};
		#endregion
	}
}