namespace RobinHood70.Robby;

using System;
using RobinHood70.CommonCode;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Properties;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon;

/// <summary>In addition to regular page data, stores information about a category.</summary>
/// <seealso cref="Page" />
public sealed class CategoryPage : Page
{
	#region Constructors

	/// <summary>Initializes a new instance of the <see cref="CategoryPage" /> class.</summary>
	/// <param name="title">The <see cref="Title"/> to copy values from.</param>
	/// <param name="options">The load options used for this page. Can be used to detect if default-valued information is legitimate or was never loaded.</param>
	/// <param name="apiItem">The API item to extract information from.</param>
	internal CategoryPage(Title title, PageLoadOptions options, IApiTitle? apiItem)
		: base(title, options, apiItem)
	{
		if (title.Namespace.Id != MediaWikiNamespaces.Category)
		{
			throw new ArgumentException(
				Globals.CurrentCulture(Resources.NamespaceMustBe, this.Title.Site[MediaWikiNamespaces.Category].Name),
				nameof(title))
			;
		}

		if (apiItem is PageItem pageItem &&
			pageItem.CategoryInfo is CategoryInfoResult catInfo)
		{
			this.FileCount = catInfo.Files;
			this.FullCount = catInfo.Size;
			this.Hidden = catInfo.Hidden;
			this.PageCount = catInfo.Pages;
			this.SubcategoryCount = catInfo.Subcategories;
		}
	}
	#endregion

	#region Public Properties

	/// <summary>Gets the number of files in the category.</summary>
	/// <value>The file count.</value>
	public int FileCount { get; }

	/// <summary>Gets the total number of entries in the category, regardless of type.</summary>
	/// <value>The full count.</value>
	public int FullCount { get; }

	/// <summary>Gets a value indicating whether this <see cref="CategoryPage" /> is hidden.</summary>
	/// <value><see langword="true" /> if hidden; otherwise, <see langword="false" />.</value>
	public bool Hidden { get; }

	/// <summary>Gets the number of pages in the category. Files and subcategories are not included in the count.</summary>
	/// <value>The page count.</value>
	public int PageCount { get; }

	/// <summary>Gets the subcategory count.</summary>
	/// <value>The subcategory count.</value>
	public int SubcategoryCount { get; }
	#endregion
}