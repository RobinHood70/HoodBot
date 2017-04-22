#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	public class QueryPageItem : ITitleOnly
	{
		#region Public Properties
		public IReadOnlyDictionary<string, string> DatabaseResults { get; set; }

		public int? Namespace { get; set; }

		public DateTime? Timestamp { get; set; }

		public string Title { get; set; }

		public string Value { get; set; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}
