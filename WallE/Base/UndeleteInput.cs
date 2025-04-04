﻿#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System;
using System.Collections.Generic;

public class UndeleteInput
{
	#region Constructors
	public UndeleteInput(string title)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(title);
		this.Title = title;
	}
	#endregion

	#region Public Properties
	public IEnumerable<int>? FileIds { get; set; }

	public string? Reason { get; set; }

	public IEnumerable<string>? Tags { get; set; }

	public IEnumerable<DateTime>? Timestamps { get; set; }

	public string Title { get; }

	public string? Token { get; set; }

	public WatchlistOption Watchlist { get; set; }
	#endregion
}