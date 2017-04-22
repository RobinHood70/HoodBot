#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class CustomResult
	{
		#region Constructors
		public CustomResult(string result) => this.Result = result;
		#endregion

		#region Public Properties
		public string Result { get; set; }
		#endregion
	}
}