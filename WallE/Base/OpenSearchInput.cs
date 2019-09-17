#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.WallE.Properties;

	#region Public Enumerations
	public enum SearchProfile
	{
		Fuzzy,
		Classic,
		Normal,
		Strict
	}

	public enum OpenSearchRedirect
	{
		Return,
		Resolve
	}
	#endregion

	public class OpenSearchInput
	{
		#region Constructor
		public OpenSearchInput(string search)
		{
			if (string.IsNullOrWhiteSpace(search))
			{
				throw new ArgumentException(Messages.InvalidSearchString, search);
			}

			this.Search = search;
		}
		#endregion

		#region Public Properties
		public int Limit { get; set; }

		public IEnumerable<int> Namespaces { get; set; }

		public SearchProfile Profile { get; set; }

		public OpenSearchRedirect Redirects { get; set; }

		public string Search { get; }

		public bool Suggest { get; set; }
		#endregion
	}
}
