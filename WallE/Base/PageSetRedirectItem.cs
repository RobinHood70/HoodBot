#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	public class PageSetRedirectItem
	{
		#region Constructors
		internal PageSetRedirectItem(string title, string? fragment, string? interwiki, IReadOnlyDictionary<string, object> generatorInfo)
		{
			this.Title = title;
			this.Fragment = fragment;
			this.Interwiki = interwiki;
			this.GeneratorInfo = generatorInfo;
		}
		#endregion

		#region Public Properties
		public string? Fragment { get; }

		// Sample query to get generator info: https://en.wikipedia.org/w/api.php?action=query&generator=prefixsearch&gpssearch=allsta&gpslimit=500&redirects
		public IReadOnlyDictionary<string, object> GeneratorInfo { get; }

		public string? Interwiki { get; }

		public string Title { get; }
		#endregion
	}
}
