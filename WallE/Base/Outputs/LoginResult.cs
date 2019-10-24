#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using RobinHood70.WikiCommon;

	public class LoginResult
	{
		#region Constructors
		internal LoginResult(string result, string? reason, string? user, long userId)
			: this(result, reason, user, userId, null, TimeSpan.Zero)
		{
		}

		internal LoginResult(string result, string? reason, string? user, long userId, string? token, TimeSpan waitTime)
		{
			this.Reason = reason;
			this.UserId = userId;
			this.User = user;
			this.Result = result;
			this.Token = token;
			this.WaitTime = waitTime;
		}
		#endregion

		#region Public Properties
		public string? Reason { get; }

		public string Result { get; }

		public string? Token { get; }

		public string? User { get; }

		public long UserId { get; }

		public TimeSpan WaitTime { get; }
		#endregion

		#region Public Static Methods
		public static LoginResult AlreadyLoggedIn(long userId, string userName) => new LoginResult(
			result: "Success",
			reason: "Already logged in",
			user: userName,
			userId: userId);

		public static LoginResult EditingAnonymously(string? userName) => new LoginResult(
			result: "Success",
			reason: "Editing anonymously",
			user: userName,
			userId: 0);
		#endregion

		#region Public Override Methods
		public override string ToString() => this.User ?? Globals.Unknown;
		#endregion
	}
}