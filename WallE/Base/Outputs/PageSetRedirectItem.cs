#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;
	using RobinHood70.WikiCommon;

	public class PageSetRedirectItem : IApiTitle
	{
		#region Constructors
		internal PageSetRedirectItem(string title, string? fragment, string? interwiki, IReadOnlyDictionary<string, object> generatorInfo)
		{
			this.FullPageName = title;
			this.Fragment = fragment;
			this.Interwiki = interwiki;
			this.GeneratorInfo = generatorInfo;
		}
		#endregion

		#region Public Properties
		public string? Fragment { get; }

		public string FullPageName { get; }

		// Sample query to get generator info: https://en.wikipedia.org/w/api.php?action=query&generator=prefixsearch&gpssearch=allsta&gpslimit=500&redirects
		public IReadOnlyDictionary<string, object> GeneratorInfo { get; }

		public string? Interwiki { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Interwiki + this.FullPageName + (this.Fragment == null ? null : '#' + this.Fragment);
		#endregion
	}
}
