namespace RobinHood70.Robby.Design
{
	using System;

	/// <summary>Interface for Title-like objects.</summary>
	public interface IWikiTitle
	{
		#region Properties

		/// <summary>Gets the full name of the page.</summary>
		/// <value>The full name of the page.</value>
		string FullPageName { get; }

		/// <summary>Gets the key to use in dictionary lookups. Usually, this is the same as the full page name before any page moves or redirects were applied.</summary>
		/// <value>The key.</value>
		string Key { get; }

		/// <summary>Gets the namespace the page is in.</summary>
		/// <value>The namespace.</value>
		Namespace Namespace { get; }

		/// <summary>Gets the name of the page without the namespace.</summary>
		/// <value>The name of the page without the namespace.</value>
		string PageName { get; }

		/// <summary>Gets the site the page is on.</summary>
		/// <value>The site.</value>
		Site Site { get; }
		#endregion
	}
}