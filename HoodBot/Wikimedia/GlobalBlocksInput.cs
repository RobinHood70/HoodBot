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

public class GlobalBlocksInput : ILimitableInput
{
	#region Constructors
	public GlobalBlocksInput()
	{
	}

	public GlobalBlocksInput(IPAddress ip)
	{
		this.IP = ip;
	}

	public GlobalBlocksInput(IEnumerable<string> addresses)
	{
		this.Addresses = addresses;
	}
	#endregion

	#region Public Properties
	public IEnumerable<string>? Addresses { get; set; }

	public DateTime? End { get; set; }

	public IEnumerable<long>? Ids { get; set; }

	public IPAddress? IP { get; }

	public int Limit { get; set; }

	public int MaxItems { get; set; }

	public GlobalBlocksProperties Properties { get; set; }

	public bool SortAscending { get; set; }

	public DateTime? Start { get; set; }
	#endregion
}