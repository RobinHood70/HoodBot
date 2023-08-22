#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.WikiCommon;

	public class QueryPageItem : IApiTitle
	{
		#region Constructors
		internal QueryPageItem(int ns, string title, string? value, IReadOnlyDictionary<string, object?>? databaseResult, DateTime? timestamp)
		{
			this.Namespace = ns;
			this.Title = title;
			this.Value = value;
			this.DatabaseResult = databaseResult;
			this.Timestamp = timestamp;
		}
		#endregion

		#region Public Properties
		public IReadOnlyDictionary<string, object?>? DatabaseResult { get; }

		public int Namespace { get; }

		public DateTime? Timestamp { get; }

		public string Title { get; }

		public string? Value { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}