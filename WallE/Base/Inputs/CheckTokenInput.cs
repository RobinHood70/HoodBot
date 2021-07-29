#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using RobinHood70.CommonCode;

	public class CheckTokenInput
	{
		#region Constructors
		public CheckTokenInput(string type, string token)
		{
			this.Type = type.NotNullOrWhiteSpace(nameof(type));
			this.Token = token.NotNullOrWhiteSpace(nameof(token));
		}
		#endregion

		#region Public Properties
		public int MaxTokenAge { get; set; }

		public string Token { get; }

		public string Type { get; }
		#endregion
	}
}
