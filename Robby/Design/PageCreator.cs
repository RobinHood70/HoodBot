namespace RobinHood70.Robby.Design
{
	using System.Collections.Generic;
	using RobinHood70.WallE.Base;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Provides a base class for creating Page objects. This serves as a go-between for customized page extensions in WallE and Robby.</summary>
	public abstract class PageCreator
	{
		#region Public Static Properties

		/// <summary>Gets a global instance of the default page creator.</summary>
		/// <value>The default page creator.</value>
		public static DefaultPageCreator Default { get; } = new DefaultPageCreator();
		#endregion

		#region Public Methods

		/// <summary>Gets regular and custom property inputs.</summary>
		/// <param name="options">Page load options.</param>
		/// <returns>A list of property inputs, including any customized property inputs required.</returns>
		/// <seealso cref="AddCustomPropertyInputs" />
		public IList<IPropertyInput> GetPropertyInputs(PageLoadOptions options)
		{
			ThrowNull(options, nameof(options));
			var whatToLoad = options.Modules;
			var propertyInputs = new List<IPropertyInput>();
			if (whatToLoad.HasFlag(PageModules.Categories))
			{
				propertyInputs.Add(new CategoriesInput());
			}

			if (whatToLoad.HasFlag(PageModules.CategoryInfo))
			{
				propertyInputs.Add(new CategoryInfoInput());
			}

			if (whatToLoad.HasFlag(PageModules.Custom))
			{
				this.AddCustomPropertyInputs(propertyInputs);
			}

			if (whatToLoad.HasFlag(PageModules.FileInfo))
			{
				propertyInputs.Add(new ImageInfoInput()
				{
					MaxItems = options.FileRevisionCount,
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

			if (whatToLoad.HasFlag(PageModules.Info))
			{
				propertyInputs.Add(new InfoInput());
			}

			if (whatToLoad.HasFlag(PageModules.Links))
			{
				propertyInputs.Add(new LinksInput());
			}

			if (whatToLoad.HasFlag(PageModules.LinksHere))
			{
				propertyInputs.Add(new LinksHereInput());
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

			if (whatToLoad.HasFlag(PageModules.TranscludedIn))
			{
				propertyInputs.Add(new TranscludedInInput());
			}

			return propertyInputs;
		}
		#endregion

		#region Public Abstract Methods

		/// <summary>Creates a page.</summary>
		/// <param name="simpleTitle">The <see cref="ISimpleTitle"/> object that represents the page to create.</param>
		/// <returns>A fully populated Page object.</returns>
		public abstract Page CreatePage(ISimpleTitle simpleTitle);

		/// <summary>Creates a page item.</summary>
		/// <param name="ns">The namespace.</param>
		/// <param name="title">The title.</param>
		/// <param name="pageId">The page identifier.</param>
		/// <returns>A new PageItem for use by WallE.</returns>
		public abstract PageItem CreatePageItem(int ns, string title, long pageId);
		#endregion

		#region Protected Abstract Methods

		/// <summary>Adds any custom property inputs.</summary>
		/// <param name="propertyInputs">The property inputs.</param>
		protected abstract void AddCustomPropertyInputs(IList<IPropertyInput> propertyInputs);
		#endregion
	}
}