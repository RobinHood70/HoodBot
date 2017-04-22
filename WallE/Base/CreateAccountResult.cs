#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	public class CreateAccountResult
	{
		#region Public Properties
		public string Result { get; set; }

		public long UserId { get; set; }

		public string UserName { get; set; }

		public IReadOnlyList<WarningsItem> Warnings { get; set; }
		#endregion
	}
}
