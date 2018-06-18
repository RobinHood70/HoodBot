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

		/// <summary>Gets the redirect fragment, if any.</summary>
		/// <value>The fragment.</value>
		public string Fragment { get; }

		/// <summary>Gets the interwiki portion of the redirect for external redirects.</summary>
		/// <value>The interwiki portion of the redirect.</value>
		public string Interwiki { get; }
	}
}
