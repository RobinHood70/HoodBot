﻿namespace RobinHood70.Robby
{
	using System;

	public class RecentChangesOptions
	{
		public DateTime? End { get; set; }

		public bool ExcludeUser { get; set; }

		public RecentChangesFilters Hide { get; set; }

		public int? Namespace { get; set; }

		public bool Newer { get; set; }

		public RecentChangesFilters ShowOnly { get; set; }

		public DateTime? Start { get; set; }

		public string Tag { get; set; }

		public RecentChangesTypes Types { get; set; }

		public string User { get; set; }
	}
}