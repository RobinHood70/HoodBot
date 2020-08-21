namespace RobinHood70.Robby.Design
{
	using System.Collections.Generic;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;
	using static RobinHood70.CommonCode.Globals;

	/// <summary>The default page creation mechanism.</summary>
	/// <seealso cref="PageCreator" />
	public class DefaultPageCreator : PageCreator
	{
		#region Public Override Methods

		/// <summary>Creates a page.</summary>
		/// <param name="title">The <see cref="ISimpleTitle" /> object that represents the page to create.</param>
		/// <returns>A fully populated Page object.</returns>
		public override Page CreatePage(ISimpleTitle title)
		{
			ThrowNull(title, nameof(title));
			return title.Namespace.Id switch
			{
				MediaWikiNamespaces.MediaWiki => new MessagePage(title),
				MediaWikiNamespaces.File => new FilePage(title),
				MediaWikiNamespaces.Category => new CategoryPage(title),
				_ => new Page(title),
			};
		}

		/// <summary>Creates a page item.</summary>
		/// <param name="ns">The namespace.</param>
		/// <param name="title">The title.</param>
		/// <param name="pageId">The page identifier.</param>
		/// <returns>A new PageItem for use by WallE.</returns>
		public override PageItem CreatePageItem(int ns, string title, long pageId) => new PageItem(ns, title, pageId);
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