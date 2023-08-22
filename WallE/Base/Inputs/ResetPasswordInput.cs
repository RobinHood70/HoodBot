#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class ResetPasswordInput
	{
		#region Constructors
		public ResetPasswordInput(string user)
		{
			this.User = user;
		}

		private ResetPasswordInput()
		{
		}
		#endregion

		#region Public Properties
		public bool Capture { get; set; }

		public string? Email { get; private set; }

		public string? Token { get; set; }

		public string? User { get; }
		#endregion

		#region Public Static Methods
		public static ResetPasswordInput FromEmail(string email) => new() { Email = email };
		#endregion
	}
}