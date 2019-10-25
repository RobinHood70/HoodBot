#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using RobinHood70.WikiCommon;

	public class CustomResult
	{
		#region Constructors
		internal CustomResult(string? result) => this.Result = result;
		#endregion

		#region Public Properties
		public string? Result { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Result ?? Globals.Unknown;
		#endregion
	}
}