﻿#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;
	using RobinHood70.CommonCode;

	public class RollbackInput
	{
		#region Constructors
		public RollbackInput(string title, string user)
		{
			this.Title = title.NotNullOrWhiteSpace(nameof(title));
			this.User = user.NotNullOrWhiteSpace(nameof(user));
		}

		public RollbackInput(long pageId, string user)
		{
			this.PageId = pageId;
			this.User = user.NotNullOrWhiteSpace(nameof(user));
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
}
