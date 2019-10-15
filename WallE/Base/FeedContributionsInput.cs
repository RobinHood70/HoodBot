#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;
	using static RobinHood70.WallE.ProjectGlobals;

	public class FeedContributionsInput
	{
		#region Constructors
		public FeedContributionsInput(string user)
		{
			ThrowNullOrWhiteSpace(user, nameof(user));
			this.User = user;
		}
		#endregion

		#region Public Properties
		public bool DeletedOnly { get; set; }

		public string? FeedFormat { get; set; }

		public int Month { get; set; }

		public int Namespace { get; }

		public bool NewOnly { get; set; }

		public bool ShowSizeDifference { get; set; }

		public IEnumerable<string>? TagFilter { get; set; }

		public bool TopOnly { get; set; }

		public string User { get; }

		public int Year { get; set; }
		#endregion
	}
}
