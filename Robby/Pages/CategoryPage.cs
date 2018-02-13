namespace RobinHood70.Robby.Pages
{
	using WallE.Base;
	using static WikiCommon.Globals;

	/// <summary>Represents a category page. The additional properties of this class will only be populated if CategoryInfo is specified in the LoadOptions property.</summary>
	/// <seealso cref="Page" />
	public class CategoryPage : Page
	{
		#region Constructors
		/// <summary>Initializes a new instance of the <see cref="CategoryPage"/> class.</summary>
		/// <param name="site">The site object.</param>
		/// <param name="fullPageName">The full name of the page, including namespace.</param>
		public CategoryPage(Site site, string fullPageName)
			: base(site, fullPageName)
		{
		}
		#endregion

		#region Public Properties
		/// <summary>Gets the file count for the category.</summary>
		public int FileCount { get; private set; }

		/// <summary>Gets the full count of pages in the category.</summary>
		public int FullCount { get; private set; }

		/// <summary>Gets a value indicating whether this <see cref="CategoryPage">category</see> is hidden.</summary>
		public bool Hidden { get; private set; }

		/// <summary>Gets the count of pages in the category, excluding files and subcategories.</summary>
		public int PageCount { get; private set; }

		/// <summary>Gets the subcategory count.</summary>
		public int SubcategoryCount { get; private set; }
		#endregion

		#region Protected Override Methods
		/// <summary>Populates <see cref="CategoryPage"/>-specific properties.</summary>
		/// <param name="pageItem">The WallE page item.</param>
		protected override void PopulateCustomResults(PageItem pageItem)
		{
			ThrowNull(pageItem, nameof(pageItem));
			var catInfo = pageItem.CategoryInfo;
			if (catInfo != null)
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