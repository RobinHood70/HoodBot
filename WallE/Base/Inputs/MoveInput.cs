namespace RobinHood70.WallE.Base
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
	using RobinHood70.CommonCode;

	public class MoveInput
	{
		#region Constructors
		public MoveInput(string from, string to)
		{
			this.From = from.NotNullOrWhiteSpace(nameof(from));
			this.To = to.NotNullOrWhiteSpace(nameof(to));
		}

		public MoveInput(long from, string to)
		{
			this.FromId = from;
			this.To = to.NotNullOrWhiteSpace(nameof(to));
		}
		#endregion

		#region Public Properties
		public string? From { get; }

		public long FromId { get; }

		public bool IgnoreWarnings { get; set; }

		public bool MoveSubpages { get; set; }

		public bool MoveTalk { get; set; }

		public bool NoRedirect { get; set; }

		public string? Reason { get; set; }

		public string? Token { get; set; }

		public string? To { get; }

		public WatchlistOption Watchlist { get; set; }
		#endregion
	}
}
