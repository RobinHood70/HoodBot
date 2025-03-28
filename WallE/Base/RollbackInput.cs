﻿#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System;
using System.Collections.Generic;

public class RollbackInput
{
	#region Constructors
	public RollbackInput(string title, string user)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(title);
		ArgumentException.ThrowIfNullOrWhiteSpace(user);
		this.Title = title;
		this.User = user;
	}

	public RollbackInput(long pageId, string user)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(user);
		this.PageId = pageId;
		this.User = user;
	}
	#endregion

	#region Public Properties
	public bool MarkBot { get; set; }

	public long PageId { get; }

	public string? Summary { get; set; }

	public IEnumerable<string>? Tags { get; set; }

	public string? Title { get; }

	public string? Token { get; set; }

	public string User { get; }

	public WatchlistOption Watchlist { get; set; }
	#endregion
}