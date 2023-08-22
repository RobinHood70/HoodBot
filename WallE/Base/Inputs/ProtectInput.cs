#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;
	using RobinHood70.CommonCode;

	public class ProtectInput
	{
		#region Constructors
		public ProtectInput(string title)
		{
			this.Title = title.NotNullOrWhiteSpace();
		}

		public ProtectInput(long pageId)
		{
			this.PageId = pageId;
		}
		#endregion

		#region Public Properties
		public bool Cascade { get; set; }

		public long PageId { get; }

		public IEnumerable<ProtectInputItem>? Protections { get; set; }

		public string? Reason { get; set; }

		public IEnumerable<string>? Tags { get; set; }

		public string? Title { get; }

		public string? Token { get; set; }

		public bool Watch { get; set; }

		public WatchlistOption Watchlist { get; set; }
		#endregion
	}
}