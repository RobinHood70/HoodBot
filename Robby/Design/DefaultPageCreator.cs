namespace RobinHood70.Robby
{
	using System.Collections.Generic;
	using Design;
	using Pages;
	using WallE.Base;
	using static WikiCommon.Globals;

	/// <summary>The default page creation mechanism.</summary>
	/// <seealso cref="RobinHood70.Robby.Design.PageCreator" />
	public class DefaultPageCreator : PageCreator
	{
		#region Public Override Methods

		/// <summary>Creates a page.</summary>
		/// <param name="site">The site the page is from.</param>
		/// <param name="title">The title of the page.</param>
		/// <returns>A fully populated Page object.</returns>
		public override Page CreatePage(Site site, string title)
		{
			ThrowNull(site, nameof(site));
			ThrowNull(title, nameof(title));
			switch (site.NamespaceFromName(title).Id)
			{
				case MediaWikiNamespaces.MediaWiki:
					return new Message(site, title);
				case MediaWikiNamespaces.File:
					return new FilePage(site, title);
				case MediaWikiNamespaces.Category:
					return new Category(site, title);
			}

			return new Page(site, title);
		}

		/// <summary>Creates a page item.</summary>
		/// <returns>A new PageItem for use by WallE.</returns>
		public override PageItem CreatePageItem() => new PageItem();
		#endregion

		#region Protected Override Methods

		/// <summary>Adds any custom property inputs.</summary>
		/// <param name="propertyInputs">The property inputs.</param>
		protected override void AddCustomPropertyInputs(IList<IPropertyInput> propertyInputs)
		{
			// The default creator requires no custom inputs, by defnition, so this is a null function.
		}
		#endregion
	}
}