namespace RobinHood70.Robby;

using System;
using RobinHood70.WallE.Base;
using RobinHood70.WallE.Eve.Modules;

/// <summary>In addition to regular page data, stores information about a category.</summary>
/// <seealso cref="Page" />
public sealed class CategoriesPageModule
{
	#region Public Constants

	/// <summary>Gets the property name for the categories module.</summary>
	public const string PropertyName = PropCategoryInfo.ModuleName;
	#endregion

	#region Constructors

	/// <summary>Initializes a new instance of the <see cref="CategoriesPageModule" /> class.</summary>
	/// <param name="catInfo">The API item to extract information from.</param>
	public CategoriesPageModule(CategoryInfoResult catInfo)
	{
		ArgumentNullException.ThrowIfNull(catInfo);
		this.FileCount = catInfo.Files;
		this.FullCount = catInfo.Size;
		this.Hidden = catInfo.Hidden;
		this.PageCount = catInfo.Pages;
		this.SubcategoryCount = catInfo.Subcategories;
	}
	#endregion

	#region Public Properties

	/// <summary>Gets the number of files in the category.</summary>
	/// <value>The file count.</value>
	public int FileCount { get; }

	/// <summary>Gets the total number of entries in the category, regardless of type.</summary>
	/// <value>The full count.</value>
	public int FullCount { get; }

	/// <summary>Gets a value indicating whether this category is hidden.</summary>
	/// <value><see langword="true" /> if hidden; otherwise, <see langword="false" />.</value>
	public bool Hidden { get; }

	/// <summary>Gets the number of pages in the category. Files and subcategories are not included in the count.</summary>
	/// <value>The page count.</value>
	public int PageCount { get; }

	/// <summary>Gets the subcategory count.</summary>
	/// <value>The subcategory count.</value>
	public int SubcategoryCount { get; }
	#endregion

	#region CategoriesCustom

	/// <summary>Parses the result of a category information query and returns a <see cref="CategoriesPageModule" /> instance.</summary>
	/// <param name="result">The result to parse.</param>
	/// <returns>A new <see cref="CategoriesPageModule" /> instance.</returns>
	/// <exception cref="InvalidOperationException">Thrown when the result is not of the expected type.</exception>
	public static (string Key, object Value) ParseCategoryInfoResult(object result) => result is CategoryInfoResult categories
		? (PropertyName, new CategoriesPageModule(categories))
		: throw new InvalidOperationException($"Unexpected result type: {result?.GetType().FullName ?? "null"}");
	#endregion
}