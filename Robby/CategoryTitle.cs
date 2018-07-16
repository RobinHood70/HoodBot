namespace RobinHood70.Robby
{
	using RobinHood70.Robby.Design;

	/// <summary>Stores information about a category link. This includes the sort key and whether or not the category is hidden.</summary>
	/// <seealso cref="Title" />
	public class CategoryTitle : Title
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="CategoryTitle"/> class.</summary>
		/// <param name="simpleTitle">The <see cref="ISimpleTitle"/> that represents the category.</param>
		/// <param name="sortKey">The sort key.</param>
		/// <param name="hidden">if set to <see langword="true" /> if the category is hidden.</param>
		internal CategoryTitle(ISimpleTitle simpleTitle, string sortKey, bool hidden)
			: base(simpleTitle)
		{
			this.Hidden = hidden;
			this.SortKey = sortKey;
		}
		#endregion

		#region Public Properties

		/// <summary>Gets a value indicating whether this <see cref="CategoryTitle"/> is hidden.</summary>
		/// <value><see langword="true" /> if hidden; otherwise, <see langword="false" />.</value>
		public bool Hidden { get; }

		/// <summary>Gets the sort key.</summary>
		/// <value>The sort key.</value>
		public string SortKey { get; }
		#endregion
	}
}
