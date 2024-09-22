#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	public class QueryPageInput : ILimitableInput, IGeneratorInput
	{
		#region Constructors
		public QueryPageInput(string page)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(page);
			this.Page = page;
		}
		#endregion

		#region Public Properties
		public int Limit { get; set; }

		public int MaxItems { get; set; }

		public string Page { get; }

		public IReadOnlyDictionary<string, string>? Parameters { get; set; }
		#endregion
	}
}