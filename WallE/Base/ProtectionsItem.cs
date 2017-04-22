#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	public class ProtectionsItem
	{
		#region Public Properties
		public bool Cascading { get; set; }

		public DateTime? Expiry { get; set; }

		public string Level { get; set; }

		public string Source { get; set; }

		public string Type { get; set; }
		#endregion
	}
}
