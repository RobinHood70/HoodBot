namespace RobinHood70.HoodBot.Wikimedia;

using System;
using System.Collections.Generic;
using System.Net;
using RobinHood70.WallE.Base;

#region Public Enumerations
[Flags]
public enum GlobalBlocksProperties
{ // (id|address|by|timestamp|expiry|reason|range)
	None = 0,
	Id = 1,
	Address = 1 << 1,
	By = 1 << 2,
	Timestamp = 1 << 3,
	Expiry = 1 << 4,
	Reason = 1 << 5,
	Range = 1 << 6,
	All = Id | Address | By | Timestamp | Expiry | Reason | Range
}
#endregion

public sealed record GlobalBlocksInput(IEnumerable<string>? Addresses, DateTime? End, IEnumerable<long>? Ids, IPAddress? IP, GlobalBlocksProperties Properties, bool SortAscending, DateTime? Start) : ILimitableInput
{
	#region Constructors
	public GlobalBlocksInput()
		: this(null, null, null, null, default, default, null)
	{
	}

	public GlobalBlocksInput(IPAddress ip)
		: this(null, null, null, ip, default, default, null)
	{
	}

	public GlobalBlocksInput(IEnumerable<string> addresses)
		: this(addresses, null, null, null, default, default, null)
	{
	}
	#endregion

	#region Public Properties
	public int Limit { get; set; }

	public int MaxItems { get; set; }
	#endregion
}