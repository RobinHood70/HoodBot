namespace RobinHood70.Robby.Design
{
	/// <summary>Interface for Title-like objects.</summary>
	public interface ISimpleTitle
	{
		/// <summary>Gets the full page name of a title.</summary>
		/// <returns>The full page name (<c>{{FULLPAGENAME}}</c>) of a title.</returns>
		string FullPageName { get; }

		/// <summary>Gets the namespace object for the title.</summary>
		/// <value>The namespace.</value>
		Namespace Namespace { get; }

		/// <summary>Gets the name of the page without the namespace.</summary>
		/// <value>The name of the page without the namespace.</value>
		string PageName { get; }
	}
}