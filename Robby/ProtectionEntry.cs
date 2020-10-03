namespace RobinHood70.Robby
{
	using System;
	using RobinHood70.WallE.Base;
	using static RobinHood70.CommonCode.Globals;

	/// <summary>Contains information about page protections.</summary>
	/// <remarks>Currently, only the level and expiry date are available. Cascading is rare, but can be added if required.</remarks>
	public sealed class ProtectionEntry
	{
		/// <summary>Initializes a new instance of the <see cref="ProtectionEntry"/> class.</summary>
		/// <param name="item">The <see cref="ProtectionsItem"/> to copy information from.</param>
		public ProtectionEntry(ProtectionsItem item)
		{
			ThrowNull(item, nameof(item));
			this.Expiry = item.Expiry;
			this.Level = item.Level;
		}

		/// <summary>Gets the protection expiry date.</summary>
		/// <value>The expiry date.</value>
		public DateTime? Expiry { get; }

		/// <summary>Gets the protection level.</summary>
		/// <value>The protection level (typically <c>autoconfirmed</c> or <c>sysop</c>, but custom values are also possible.</value>
		public string Level { get; }
	}
}