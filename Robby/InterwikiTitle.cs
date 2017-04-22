namespace RobinHood70.Robby
{
	using System;
	using WallE.Base;

	public class InterwikiTitle : Title
	{
		internal InterwikiTitle(Site site, InterwikiTitleItem baseItem)
			: base(site, baseItem.Title)
		{
			this.Prefix = baseItem.InterwikiPrefix;
			this.Uri = baseItem.Url;
		}

		public string Prefix { get; }

		public Uri Uri { get; }
	}
}
