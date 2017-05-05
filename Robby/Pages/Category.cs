namespace RobinHood70.Robby.Pages
{
	using Design;
	using WallE.Base;
	using static WikiCommon.Globals;

	public class Category : Page
	{
		public Category(Site site, string fullPageName, PageLoadOptions loadOptions)
			: base(site, fullPageName, loadOptions)
		{
		}

		public int FileCount { get; private set; }

		public int FullCount { get; private set; }

		public bool Hidden { get; private set; }

		public int PageCount { get; private set; }

		public int SubcategoryCount { get; private set; }

		public override void Populate(PageItem pageItem)
		{
			ThrowNull(pageItem, nameof(pageItem));
			base.Populate(pageItem);

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
	}
}
