#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System;

public class ProtectResultItem
{
	#region Constructors
	internal ProtectResultItem(string type, string level, DateTime? expiry)
	{
		this.Type = type;
		this.Level = level;
		this.Expiry = expiry;
	}
	#endregion

	#region Public Properties
	public DateTime? Expiry { get; }

	public string Level { get; }

	public string Type { get; }
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Type + '=' + this.Level;
	#endregion
}