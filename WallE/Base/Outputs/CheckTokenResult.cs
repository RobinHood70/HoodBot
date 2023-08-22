#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	public class CheckTokenResult
	{
		#region Constructors
		internal CheckTokenResult(string result, DateTime? generated)
		{
			this.Result = result;
			this.Generated = generated;
		}
		#endregion

		#region Public Properties
		public DateTime? Generated { get; }

		public string Result { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Result;
		#endregion
	}
}