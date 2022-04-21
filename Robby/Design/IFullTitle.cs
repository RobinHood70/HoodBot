namespace RobinHood70.Robby.Design
{
	/// <summary>Represents full title information, including interwiki, namespace, page name, and fragment.</summary>
	/// <seealso cref="Title" />
	public interface IFullTitle
	{
		#region Properties

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
		#endregion

		#region Public Methods

		/// <summary>Compares two objects for <see cref="Namespace"/> and <see cref="PageName"/> equality.</summary>
		/// <param name="other">The object to compare to.</param>
		/// <returns><see langword="true"/> if the Namespace and PageName match, regardless of any other properties.</returns>
		bool SimpleEquals(Title? other);
		#endregion
	}
}