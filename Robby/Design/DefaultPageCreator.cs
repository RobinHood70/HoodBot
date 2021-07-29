namespace RobinHood70.Robby.Design
{
	using System.Collections.Generic;
using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;

	/// <summary>The default page creation mechanism.</summary>
	/// <seealso cref="PageCreator" />
	public class DefaultPageCreator : PageCreator
	{
		#region Public Override Methods

		/// <summary>Creates a page.</summary>
		/// <param name="title">The <see cref="ISimpleTitle" /> object that represents the page to create.</param>
		/// <returns>A fully populated Page object.</returns>
		public override Page CreatePage(ISimpleTitle title) => title.NotNull(nameof(title)).Namespace.Id switch
		{
			MediaWikiNamespaces.MediaWiki => new MessagePage(title),
			MediaWikiNamespaces.File => new FilePage(title),
			MediaWikiNamespaces.Category => new CategoryPage(title),
			_ => new Page(title),
		};
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