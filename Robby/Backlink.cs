namespace RobinHood70.Robby
{
	/// <summary>Represents a backlink title which has been redirected from another title.</summary>
	/// <seealso cref="Title" />
	public sealed class Backlink : Title
	{
		/// <summary>Initializes a new instance of the <see cref="Backlink"/> class.</summary>
		/// <param name="title">The original page.</param>
		/// <param name="redirectTitle">The title the redirect points to.</param>
		internal Backlink(Title title, Title redirectTitle)
			: base(title)
		{
			this.RedirectTitle = redirectTitle;
		}

		/// <summary>Gets the title of the redirect page that links to this page.</summary>
		/// <value>The title of the redirect page that links to this page.</value>
		public Title RedirectTitle { get; }
	}
}