namespace RobinHood70.Robby
{
	/// <summary>Stores information about a category link. This includes the sort key and whether or not the category is hidden.</summary>
	/// <seealso cref="ITitle" />
	public class Category : ITitle
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="Category"/> class.</summary>
		/// <param name="title">The <see cref="Robby.Title"/> that represents the category.</param>
		/// <param name="sortKey">The sort key.</param>
		/// <param name="hidden">if set to <see langword="true" /> if the category is hidden.</param>
		internal Category(Title title, string? sortKey, bool hidden)
		{
			this.Title = title;
			this.SortKey = sortKey;
			this.Hidden = hidden;
		}
		#endregion

		#region Public Properties

		/// <summary>Gets a value indicating whether this <see cref="Category"/> is hidden.</summary>
		/// <value><see langword="true" /> if hidden; otherwise, <see langword="false" />.</value>
		public bool Hidden { get; }

		/// <summary>Gets the sort key.</summary>
		/// <value>The sort key.</value>
		public string? SortKey { get; }

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