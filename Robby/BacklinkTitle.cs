namespace RobinHood70.Robby
{
	/// <summary>Represents a backlink title which has been redirected from another title.</summary>
	/// <seealso cref="Title" />
	public class BacklinkTitle : Title
	{
		/// <summary>Initializes a new instance of the <see cref="BacklinkTitle"/> class.</summary>
		/// <param name="site">The site.</param>
		/// <param name="fullName">The full name.</param>
		/// <param name="redirectTitle">The title the redirect points to.</param>
		protected internal BacklinkTitle(Site site, string fullName, Title redirectTitle)
			: base(site, fullName) => this.RedirectTitle = redirectTitle;

		/// <summary>Gets the title of the redirect page that links to this page.</summary>
		/// <value>The title of the redirect page that links to this page.</value>
		public Title RedirectTitle { get; }
	}
}
