#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	public class DuplicateFileItem
	{
		#region Public Properties
		public string Name { get; set; }

		public bool Shared { get; set; }

		public DateTime? Timestamp { get; set; }

		public string User { get; set; }
		#endregion
	}
}
