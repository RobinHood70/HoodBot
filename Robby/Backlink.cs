namespace RobinHood70.Robby
{
	/// <summary>Represents a backlink title which has been redirected from another title.</summary>
	/// <seealso cref="Title" />
	public sealed class Backlink : ITitle
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="Backlink"/> class.</summary>
		/// <param name="title">The original page.</param>
		/// <param name="redirectTitle">The title the redirect points to.</param>
		internal Backlink(Title title, Title redirectTitle)
		{
			this.RedirectTitle = redirectTitle;
			this.Title = title;
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the title of the redirect page that links to this page.</summary>
		/// <value>The title of the redirect page that links to this page.</value>
		public Title RedirectTitle { get; }

		/// <inheritdoc/>
		public Title Title { get; }
		#endregion

		#region Public Methods

		/// <inheritdoc/>
		public string AsLink(LinkFormat linkFormat = LinkFormat.Plain) => this.Title.AsLink(linkFormat);

		/// <inheritdoc/>
		public string LinkName() => this.Title.LinkName();
		#endregion
	}
}