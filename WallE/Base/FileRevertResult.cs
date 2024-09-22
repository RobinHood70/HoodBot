#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	// "errors" node is not captured here because I was unable to find any way to produce errors for testing. Everything I tried threw dieUsage errors.
	public class FileRevertResult
	{
		#region Constructors
		internal FileRevertResult(string result)
		{
			this.Result = result;
		}
		#endregion

		#region Public Properties
		public string Result { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Result;
		#endregion
	}
}