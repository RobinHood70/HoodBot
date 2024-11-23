#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System.Collections.Generic;

public class DeleteInput
{
	#region Constructors
	public DeleteInput(string title)
	{
		this.Title = title;
	}

	public DeleteInput(long pageId)
	{
		this.PageId = pageId;
	}
	#endregion

	#region Public Properties
	public string? OldImage { get; set; }

	public long PageId { get; }

	public string? Reason { get; set; }

	public IEnumerable<string>? Tags { get; set; }

	public string? Title { get; }

	public string? Token { get; set; }

	public WatchlistOption Watchlist { get; set; }
	#endregion
}