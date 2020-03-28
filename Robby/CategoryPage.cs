namespace RobinHood70.Robby
{
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;
	using static RobinHood70.CommonCode.Globals;

	/// <summary>In addition to regular page data, stores information about a category.</summary>
	/// <seealso cref="Page" />
	public class CategoryPage : Page
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="CategoryPage" /> class.</summary>
		/// <param name="site">The site the category is from.</param>
		/// <param name="pageName">The page name (<em>without</em> the leading namespace).</param>
		public CategoryPage(Site site, string pageName)
			: base(site, MediaWikiNamespaces.Category, pageName)
		{
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the number of files in the category.</summary>
		/// <value>The file count.</value>
		public int FileCount { get; private set; }

		/// <summary>Gets the total number of entries in the category, regardless of type.</summary>
		/// <value>The full count.</value>
		public int FullCount { get; private set; }

		/// <summary>Gets a value indicating whether this <see cref="CategoryPage" /> is hidden.</summary>
		/// <value><see langword="true" /> if hidden; otherwise, <see langword="false" />.</value>
		public bool Hidden { get; private set; }

		/// <summary>Gets the number of pages in the category. Files and subcategories are not included in the count.</summary>
		/// <value>The page count.</value>
		public int PageCount { get; private set; }

		/// <summary>Gets the subcategory count.</summary>
		/// <value>The subcategory count.</value>
		public int SubcategoryCount { get; private set; }
		#endregion

		#region Protected Override Methods

		/// <summary>When overridden in a derived class, populates custom page properties with custom data from the WallE PageItem.</summary>
		/// <param name="pageItem">The page item.</param>
		protected override void PopulateCustomResults(PageItem pageItem)
		{
			ThrowNull(pageItem, nameof(pageItem));
			if (pageItem.CategoryInfo is CategoryInfoResult catInfo)
			{
				this.FileCount = catInfo.Files;
				this.FullCount = catInfo.Size;
				this.Hidden = catInfo.Hidden;
				this.PageCount = catInfo.Pages;
				this.SubcategoryCount = catInfo.Subcategories;
			}
		}
		#endregion
	}
}