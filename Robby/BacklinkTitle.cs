namespace RobinHood70.Robby
{
	/// <summary>Represents a backlink title which has been redirected from another title.</summary>
	/// <seealso cref="RobinHood70.Robby.Title" />
	public class BacklinkTitle : Title
	{
		internal BacklinkTitle(Site site, string fullName, Title redirectTitle)
			: base(site, fullName) => this.RedirectTitle = redirectTitle;

		/// <summary>Gets the redirect this title was redirected through.</summary>
		/// <value>The redirect this title was redirected through.</value>
		public Title RedirectTitle { get; }
	}
}
