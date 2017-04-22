#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	public class PageSetRedirectItem
	{
		#region Public Properties
		public string Fragment { get; set; }

		public IReadOnlyDictionary<string, object> GeneratorInfo { get; set; }

		public string Interwiki { get; set; }

		public string Title { get; set; }
		#endregion
	}
}
