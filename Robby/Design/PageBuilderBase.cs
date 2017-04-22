namespace RobinHood70.Robby.Design
{
	using System.Collections.Generic;
	using Pages;
	using WallE.Base;
	using static Globals;

	public abstract class PageBuilderBase
	{
		#region Public Methods
		public Page BuildPage(Site site, PageItem pageItem, PageLoadOptions options)
		{
			ThrowNull(pageItem, nameof(pageItem));
			var page = this.CreatePage(site, pageItem.Namespace.Value, pageItem.Title, options);
			this.Populate(page, pageItem);
			return page;
		}

		public IList<IPropertyInput> GetPropertyInputs(PageLoadOptions options)
		{
			ThrowNull(options, nameof(options));
			var whatToLoad = options.Modules;
			var propertyInputs = new List<IPropertyInput>();
			if (whatToLoad.HasFlag(PageModules.Categories))
			{
				propertyInputs.Add(new CategoriesInput());
			}

			if (whatToLoad.HasFlag(PageModules.Info))
			{
				propertyInputs.Add(new InfoInput());
			}

			if (whatToLoad.HasFlag(PageModules.Links))
			{
				propertyInputs.Add(new LinksInput());
			}

			if (whatToLoad.HasFlag(PageModules.Properties))
			{
				propertyInputs.Add(new PagePropertiesInput());
			}

			if (whatToLoad.HasFlag(PageModules.Revisions))
			{
				var revs = new RevisionsInput()
				{
					End = options.RevisionTo,
					EndId = options.RevisionToId,
					MaxItems = options.RevisionCount,
					SortAscending = options.RevisionNewer,
					Start = options.RevisionFrom,
					StartId = options.RevisionFromId,
					Properties =
						RevisionsProperties.Comment |
						RevisionsProperties.Content |
						RevisionsProperties.Flags |
						RevisionsProperties.Ids |
						RevisionsProperties.Sha1 |
						RevisionsProperties.Size |
						RevisionsProperties.Tags |
						RevisionsProperties.Timestamp |
						RevisionsProperties.User,
				};
				propertyInputs.Add(revs);
			}

			if (whatToLoad.HasFlag(PageModules.Templates))
			{
				propertyInputs.Add(new TemplatesInput());
			}

			if (whatToLoad.HasFlag(PageModules.FileInfo))
			{
				propertyInputs.Add(new ImageInfoInput()
				{
					MaxItems = options.ImageRevisionCount,
					Properties =
						ImageProperties.BitDepth |
						ImageProperties.Comment |
						ImageProperties.Mime |
						ImageProperties.Size |
						ImageProperties.Timestamp |
						ImageProperties.Url |
						ImageProperties.User,
				});
			}

			if (whatToLoad.HasFlag(PageModules.CategoryInfo))
			{
				propertyInputs.Add(new CategoryInfoInput());
			}

			if (whatToLoad.HasFlag(PageModules.Custom))
			{
				this.AddCustomPropertyInputs(propertyInputs);
			}

			return propertyInputs;
		}

		public void Populate(Page page, PageItem pageItem)
		{
			ThrowNull(page, nameof(page));
			ThrowNull(pageItem, nameof(pageItem));
			page.Populate(pageItem);
			this.PopulateCustom(page, pageItem);
		}
		#endregion

		#region Public Abstract Methods
		public abstract Page CreatePage(Site site, int ns, string title, PageLoadOptions options);

		public abstract PageItem CreatePageItem();
		#endregion

		#region Protected Abstract Methods
		protected abstract void AddCustomPropertyInputs(IList<IPropertyInput> propertyInputs);

		protected abstract void PopulateCustom(Page page, PageItem pageItem);
		#endregion

	}
}