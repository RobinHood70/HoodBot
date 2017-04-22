#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class CreateAccountInput
	{
		#region Constructors
		protected CreateAccountInput(string name, string email)
			: this(name) => this.Email = email;

		private CreateAccountInput(string name) => this.Name = name;
		#endregion

		#region Public Properties
		public string Domain { get; set; }

		public string Email { get; set; }

		public string Language { get; set; }

		public bool MailPassword => this.Password == null && this.Email != null;

		public string Name { get; }

		public string Password { get; private set; }

		public string RealName { get; set; }

		public string Reason { get; set; }
		#endregion

		#region Public Static Methods
		public static CreateAccountInput CreateWithPassword(string name, string password) => new CreateAccountInput(name) { Password = password };
		#endregion
	}
}
