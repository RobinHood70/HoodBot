#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	public class ProtectionsItem
	{
		#region Constructors
		internal ProtectionsItem(string type, string level, DateTime? expiry, bool cascading, string? source)
		{
			this.Type = type;
			this.Level = level;
			this.Expiry = expiry;
			this.Cascading = cascading;
			this.Source = source;
		}
		#endregion

		#region Public Properties
		public bool Cascading { get; }

		public DateTime? Expiry { get; }

		public string Level { get; }

		public string? Source { get; }

		public string Type { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Type + '=' + this.Level;
		#endregion
	}
}
