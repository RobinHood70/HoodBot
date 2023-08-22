namespace RobinHood70.WallE.Eve
{
	using System.Collections.Generic;

	/// <summary>This is a simple wrapper around an enumerable string which acts as a pseudo-enumeration of string constants for tokens.</summary>
	internal sealed class TokensInput
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="TokensInput" /> class.</summary>
		/// <param name="tokenTypes">The token types.</param>
		public TokensInput(params string[] tokenTypes)
		{
			this.Types = tokenTypes;
		}

		/// <summary>Initializes a new instance of the <see cref="TokensInput" /> class.</summary>
		/// <param name="tokenTypes">The token types.</param>
		public TokensInput(IEnumerable<string> tokenTypes)
		{
			this.Types = tokenTypes;
		}
		#endregion

		#region Public Static Properties

		/// <summary>Gets the name of the cross-site request forgery token.</summary>
		/// <value>The name of the CSRF token.</value>
		public static string Csrf => "csrf";

		/// <summary>Gets the name of the edit token.</summary>
		/// <value>The name of the edit token.</value>
		public static string Edit => "edit";

		/// <summary>Gets the name of the login token.</summary>
		/// <value>The name of the login.</value>
		public static string Login => "login";

		/// <summary>Gets the name of the patrol token.</summary>
		/// <value>The name of the patrol token.</value>
		public static string Patrol => "patrol";

		/// <summary>Gets the name of the rollback token.</summary>
		/// <value>The name of the rollback token.</value>
		public static string Rollback => "rollback";

		/// <summary>Gets the name of the user rights token.</summary>
		/// <value>The name of the user rights token.</value>
		public static string UserRights => "userrights";

		/// <summary>Gets the name of the watch token.</summary>
		/// <value>The name of the watch token.</value>
		public static string Watch => "watch";
		#endregion

		#region Public Properties

		/// <summary>Gets the combined list of tokens.</summary>
		/// <value>The combined list of tokens.</value>
		public IEnumerable<string> Types { get; }
		#endregion
	}
}