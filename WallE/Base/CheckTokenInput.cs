#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using static Globals;

	public class CheckTokenInput
	{
		#region Constructors
		public CheckTokenInput(string type, string token)
		{
			ThrowNullOrWhiteSpace(type, nameof(type));
			ThrowNullOrWhiteSpace(token, nameof(token));
			this.Type = type;
			this.Token = token;
		}
		#endregion

		#region Public Properties
		public int MaxTokenAge { get; set; }

		public string Token { get; }

		public string Type { get; }
		#endregion
	}
}
