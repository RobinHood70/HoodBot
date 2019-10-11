namespace RobinHood70.Robby.Design
{
	using System.Collections.Generic;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>The default page creation mechanism.</summary>
	/// <seealso cref="PageCreator" />
	public class DefaultPageCreator : PageCreator
	{
		#region Public Override Methods

		/// <summary>Creates a page.</summary>
		/// <param name="simpleTitle">The <see cref="ISimpleTitle" /> object that represents the page to create.</param>
		/// <returns>A fully populated Page object.</returns>
		public override Page CreatePage(ISimpleTitle simpleTitle)
		{
			ThrowNull(simpleTitle, nameof(simpleTitle));
			return simpleTitle.Namespace.Id switch
			{
				MediaWikiNamespaces.MediaWiki => new MessagePage(simpleTitle.Site, simpleTitle.PageName),
				MediaWikiNamespaces.File => new FilePage(simpleTitle.Site, simpleTitle.PageName),
				MediaWikiNamespaces.Category => new CategoryPage(simpleTitle.Site, simpleTitle.PageName),
				_ => new Page(simpleTitle),
			};
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