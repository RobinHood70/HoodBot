#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System;

public class ProtectInputItem(string type, string level)
{
	#region Public Properties
	public DateTime? Expiry { get; set; }

	public string? ExpiryRelative { get; set; }

	public string Level { get; set; } = level;

	public string Type { get; set; } = type;
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Type + '=' + this.Level;
	#endregion
}