#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)

namespace RobinHood70.WallE.Base
{
	using RobinHood70.WikiCommon;

	public class EmailUserResult
	{
		#region Constructors
		internal EmailUserResult(string result, string? message)
		{
			this.Result = result;
			this.Message = message;
		}
		#endregion

		#region Public Properties
		public string? Message { get; }

		public string Result { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Result + ": " + this.Message.Ellipsis(30);
		#endregion
	}
}