#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System;
using System.Collections.Generic;
using System.Net;
using RobinHood70.CommonCode;

#region Public Enumerations
[Flags]
public enum BlocksProperties
{
	None = 0,
	Id = 1,
	User = 1 << 1,
	UserId = 1 << 2,
	By = 1 << 3,
	ById = 1 << 4,
	Timestamp = 1 << 5,
	Expiry = 1 << 6,
	Reason = 1 << 7,
	Range = 1 << 8,
	Flags = 1 << 9,
	All = Id | User | UserId | By | ById | Timestamp | Expiry | Reason | Range | Flags
}
#endregion

public class BlocksInput : ILimitableInput
{
	#region Constructors
	public BlocksInput()
	{
	}

	public BlocksInput(IPAddress ip)
	{
		this.IP = ip;
	}

	public BlocksInput(IEnumerable<string> users)
	{
		this.Users = users;
	}
	#endregion

	#region Public Properties
	public DateTime? End { get; set; }

	public Filter FilterAccount { get; set; }

	public Filter FilterIP { get; set; }

	public Filter FilterRange { get; set; }

	public Filter FilterTemporary { get; set; }

	public IEnumerable<long>? Ids { get; set; }

	public IPAddress? IP { get; }

	public int Limit { get; set; }

	public int MaxItems { get; set; }

	public BlocksProperties Properties { get; set; }

	public bool SortAscending { get; set; }

	public DateTime? Start { get; set; }

	public IEnumerable<string>? Users { get; }
	#endregion
}