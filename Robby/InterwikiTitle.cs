namespace RobinHood70.Robby
{
	using System;
	using WallE.Base;

	/// <summary>Stores information about an interwiki title.</summary>
	/// <seealso cref="RobinHood70.Robby.Title" />
	public class InterwikiTitle : Title
	{
		internal InterwikiTitle(Site site, InterwikiTitleItem baseItem)
			: base(site, baseItem.Title)
		{
			this.Prefix = baseItem.InterwikiPrefix;
			this.Uri = baseItem.Url;
		}

		/// <summary>Gets the interwiki prefix.</summary>
		/// <value>The interwiki prefix.</value>
		public string Prefix { get; }

		/// <summary>Gets the URI of the link.</summary>
		/// <value>The URI of the link.</value>
		public Uri Uri { get; }
	}
}
