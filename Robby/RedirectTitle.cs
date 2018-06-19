namespace RobinHood70.Robby
{
	using WallE.Base;

	/// <summary>A title object that represents a redirect.</summary>
	/// <seealso cref="RobinHood70.Robby.Title" />
	public class RedirectTitle : Title
	{
		/// <summary>Initializes a new instance of the <see cref="RedirectTitle"/> class.</summary>
		/// <param name="site">The site.</param>
		/// <param name="baseItem">The <see cref="PageSetRedirectItem"/> to initialize from.</param>
		protected internal RedirectTitle(Site site, PageSetRedirectItem baseItem)
			: base(site, baseItem?.Title)
		{
			this.Fragment = baseItem.Fragment;
			this.Interwiki = baseItem.Interwiki;
		}

		/// <summary>Initializes a new instance of the <see cref="RedirectTitle"/> class.</summary>
		/// <param name="site">The site.</param>
		/// <param name="title">The title to initialize from. It is assumed that the redirect is local, possibly with a fragment; no interwiki parsing is performed.</param>
		protected internal RedirectTitle(Site site, string title)
			: base(site, title)
		{
			var split = this.PageName.Split(new[] { '#' }, 2);
			if (split.Length == 2)
			{
				this.PageName = split[0];
				this.Fragment = split[1];
			}
		}

		/// <summary>Gets the redirect fragment, if any.</summary>
		/// <value>The fragment.</value>
		public string Fragment { get; }

		/// <summary>Gets the interwiki portion of the redirect for external redirects.</summary>
		/// <value>The interwiki portion of the redirect.</value>
		public string Interwiki { get; }
	}
}
