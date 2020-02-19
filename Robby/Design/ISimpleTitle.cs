namespace RobinHood70.Robby.Design
{
	/// <summary>Interface for Title-like objects.</summary>
	public interface ISimpleTitle : ISiteSpecific
	{
		#region Properties

		/// <summary>Gets the value corresponding to {{BASEPAGENAME}}.</summary>
		/// <value>The name of the base page.</value>
		string BasePageName { get; }

		/// <summary>Gets the full name of the page.</summary>
		/// <value>The full name of the page.</value>
		string FullPageName { get; }

		/// <summary>Gets the namespace the page is in.</summary>
		/// <value>The namespace.</value>
		Namespace Namespace { get; }

		/// <summary>Gets the ID of the namespace the page is in.</summary>
		/// <value>The namespace ID.</value>
		int NamespaceId { get; }

		/// <summary>Gets the name of the page without the namespace.</summary>
		/// <value>The name of the page without the namespace.</value>
		string PageName { get; }

		/// <summary>Gets a Title object for this Title's corresponding subject page.</summary>
		/// <value>The subject page.</value>
		/// <remarks>If this Title is a subject page, returns itself.</remarks>
		ISimpleTitle SubjectPage { get; }

		/// <summary>Gets the value corresponding to {{SUBPAGENAME}}.</summary>
		/// <value>The name of the subpage.</value>
		string SubpageName { get; }

		/// <summary>Gets a Title object for this Title's corresponding subject page.</summary>
		/// <value>The talk page.</value>
		/// <remarks>If this Title is a talk page, the Title returned will be itself. Returns null for pages which have no associated talk page.</remarks>
		ISimpleTitle? TalkPage { get; }
		#endregion

		#region Methods

		/// <summary>Returns the provided title as link text.</summary>
		/// <returns>The current title, formatted as a link.</returns>
		string AsLink();

		/// <summary>Deconstructs this instance into its constituent parts.</summary>
		/// <param name="site">The site this title is from.</param>
		/// <param name="ns">The value returned by <see cref="Namespace"/>.</param>
		/// <param name="pageName">The value returned by <see cref="PageName"/>.</param>
		public void Deconstruct(out Site site, out int ns, out string pageName);

		/// <summary>Checks if the current page name is the same as the specified page name, based on the case-sensitivity for the namespace.</summary>
		/// <param name="pageName">The page name to compare to.</param>
		/// <returns><see langword="true" /> if the two string are considered the same; otherwise <see langword="false" />.</returns>
		/// <remarks>It is assumed that the namespace for the parameter is equal to the current one, or at least that they have the same case-sensitivy.</remarks>
		public bool PageNameEquals(string pageName);

		/// <summary>Indicates whether the current title is equal to another title based on Namespace and PageName only.</summary>
		/// <param name="other">A title to compare with this one.</param>
		/// <returns><see langword="true"/> if the current title is equal to the <paramref name="other"/> parameter; otherwise, <see langword="false"/>.</returns>
		/// <remarks>This method is named as it is to avoid any ambiguity about what is being checked, as well as to avoid the various issues associated with implementing IEquatable on unsealed types.</remarks>
		bool SimpleEquals(ISimpleTitle other);
		#endregion
	}
}