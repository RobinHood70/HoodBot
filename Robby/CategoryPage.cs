﻿namespace RobinHood70.Robby
{
	using System;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Properties;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;

	/// <summary>In addition to regular page data, stores information about a category.</summary>
	/// <seealso cref="Page" />
	public class CategoryPage : Page
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="CategoryPage" /> class.</summary>
		/// <param name="site">The site the category is from.</param>
		/// <param name="pageName">The page name.</param>
		public CategoryPage(Site site, string pageName)
			: base((site.NotNull(nameof(site)))[MediaWikiNamespaces.Category], pageName)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="CategoryPage" /> class.</summary>
		/// <param name="ns">The namespace (must be Category).</param>
		/// <param name="pageName">The page name.</param>
		public CategoryPage(Namespace ns, string pageName)
			: base(ns, pageName)
		{
			if (ns.Id != MediaWikiNamespaces.Category)
			{
				throw new ArgumentException(Globals.CurrentCulture(Resources.NamespaceMustBe, ns.Site[MediaWikiNamespaces.Category].Name), nameof(ns));
			}
		}

		/// <summary>Initializes a new instance of the <see cref="CategoryPage" /> class.</summary>
		/// <param name="title">The <see cref="ISimpleTitle"/> to copy values from.</param>
		public CategoryPage(ISimpleTitle title)
			: base(title)
		{
			if (title.Namespace.Id != MediaWikiNamespaces.Category)
			{
				throw new ArgumentException(Globals.CurrentCulture(Resources.NamespaceMustBe, this.Site[MediaWikiNamespaces.Category].Name), nameof(title));
			}
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
			if (pageItem.NotNull(nameof(pageItem)).CategoryInfo is CategoryInfoResult catInfo)
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