#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	public class CreateAccountResult
	{
		#region Constructors
		internal CreateAccountResult(string result, IReadOnlyDictionary<string, string> captchaData, string? token, long userId, string? userName, IReadOnlyList<WarningsItem> warnings)
		{
			this.CaptchaData = captchaData;
			this.UserName = userName;
			this.UserId = userId;
			this.Result = result;
			this.Token = token;
			this.Warnings = warnings;
		}
		#endregion

		#region Public Properties
		public IReadOnlyDictionary<string, string> CaptchaData { get; }

		public string Result { get; }

		public string? Token { get; }

		public long UserId { get; }

		public string? UserName { get; }

		public IReadOnlyList<WarningsItem> Warnings { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.UserName ?? this.Result;
		#endregion
	}
}
