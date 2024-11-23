namespace RobinHood70.Robby.Design;

using System;
using System.Collections.Generic;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon;

/// <summary>The default page creation mechanism.</summary>
/// <seealso cref="PageCreator" />
public class DefaultPageCreator : PageCreator
{
	#region Public Override Methods

	/// <summary>Creates a page.</summary>
	/// <param name="title">The <see cref="Title" /> object that represents the page to create.</param>
	/// <param name="options">The load options used for this page. Can be used to detect if default-valued information is legitimate or was never loaded.</param>
	/// <param name="apiItem">The API item to populate the page data from.</param>
	/// <returns>A fully populated Page object.</returns>
	public override Page CreatePage(Title title, PageLoadOptions options, IApiTitle? apiItem)
	{
		ArgumentNullException.ThrowIfNull(title);
		return title.Namespace.Id switch
		{
			MediaWikiNamespaces.Category => new CategoryPage(title, options, apiItem),
			MediaWikiNamespaces.File => new FilePage(title, options, apiItem),
			MediaWikiNamespaces.MediaWiki => new MessagePage(title, options, apiItem),
			_ => new Page(title, options, apiItem),
		};
	}
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