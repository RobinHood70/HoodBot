#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	public class MessageItem
	{
		#region Public Properties
		public string ForValue { get; set; }

		public string Key { get; set; }

		public IReadOnlyList<string> Parameters { get; set; }
		#endregion
	}
}
