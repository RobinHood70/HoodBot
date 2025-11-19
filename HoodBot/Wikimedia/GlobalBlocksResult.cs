namespace RobinHood70.HoodBot.Wikimedia;

using System;
using RobinHood70.CommonCode;
public sealed record GlobalBlocksResult(string? Address, bool AnonymousOnly, string? By, string? ByWiki, DateTime? Expiry, long Id, string? RangeStart, string? RangeEnd, string? Reason, DateTime? Timestamp)
{
	#region Public Override Methods
	public override string ToString() => this.Address ?? Globals.Unknown;
	#endregion
}