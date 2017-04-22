#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	public class FeedRecentChangesInput
	{
		#region Public Properties
		public bool Associated { get; set; }

		public IEnumerable<string> Categories { get; set; }

		public bool CategoriesAny { get; set; }

		public int Days { get; set; }

		public string FeedFormat { get; set; }

		public DateTime? From { get; set; }

		public bool HideAnonymous { get; set; }

		public bool HideBots { get; set; }

		public bool HideCategorization { get; set; }

		public bool HideLoggedInUsers { get; set; }

		public bool HideMinor { get; set; }

		public bool HideMyself { get; set; }

		public bool HidePatrolled { get; set; }

		public bool Invert { get; set; }

		public int Limit { get; set; }

		public int? Namespace { get; set; }

		public bool ShowLinkedTo { get; set; }

		public IEnumerable<string> TagFilter { get; set; }

		public bool Target { get; set; }
		#endregion
	}
}
