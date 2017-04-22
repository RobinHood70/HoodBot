namespace RobinHood70.Robby
{
	using WallE.Base;

	public class RedirectTitle : Title
	{
		internal RedirectTitle(Site site, PageSetRedirectItem baseItem)
			: base(site, baseItem?.Title)
		{
			this.Fragment = baseItem.Fragment;
			this.Interwiki = baseItem.Interwiki;
		}

		public string Fragment { get; }

		public string Interwiki { get; }
	}
}
