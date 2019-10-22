#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	public class InterwikiTitleItem
	{
		#region Constructors
		internal InterwikiTitleItem(string prefix, string title, Uri? url)
		{
			this.Prefix = prefix;
			this.Title = title;
			this.Url = url;
		}
		#endregion

		#region Public Properties
		public string Prefix { get; }

		public string Title { get; }

		public Uri? Url { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Prefix;
		#endregion
	}
}
