namespace RobinHood70.Robby.Design
{
	/// <summary>Represents full title information, including interwiki, namespace, page name, and fragment.</summary>
	/// <seealso cref="SimpleTitle" />
	public interface IFullTitle
	{
		/// <summary>Gets the fragment.</summary>
		/// <value>The fragment.</value>
		string? Fragment { get; }

		/// <summary>Gets the interwiki entry.</summary>
		/// <value>The interwiki entry.</value>
		InterwikiEntry? Interwiki { get; }

		/// <summary>Gets the namespace object for the title.</summary>
		/// <value>The namespace.</value>
		Namespace Namespace { get; }

		/// <summary>Gets the name of the page without the namespace.</summary>
		/// <value>The name of the page without the namespace.</value>
		string PageName { get; }
	}
}