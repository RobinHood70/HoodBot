#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	public class ProtectInputItem
	{
		#region Constructors
		public ProtectInputItem(string type, string level)
		{
			this.Type = type;
			this.Level = level;
		}
		#endregion

		#region Public Properties
		public DateTime? Expiry { get; set; }

		public string? ExpiryRelative { get; set; }

		public string Level { get; set; }

		public string Type { get; set; }
		#endregion
	}
}