namespace RobinHood70.Robby.Design;

using System;
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
		return new Page(title, options, apiItem);
	}
	#endregion
}