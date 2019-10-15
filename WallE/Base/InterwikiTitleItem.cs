#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	public class InterwikiTitleItem
	{
		#region Constructors
		public InterwikiTitleItem(string interwikiPrefix, string title, Uri? uri)
		{
			this.InterwikiPrefix = interwikiPrefix;
			this.Title = title;
			this.Url = uri;
		}
		#endregion

		#region Public Properties
		public string InterwikiPrefix { get; }

		public string Title { get; }

		public Uri Url { get; }
		#endregion
	}
}
