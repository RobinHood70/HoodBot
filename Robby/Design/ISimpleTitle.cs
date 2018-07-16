namespace RobinHood70.Robby.Design
{
	/// <summary>Interface for Title-like objects.</summary>
	public interface ISimpleTitle
	{
		#region Properties

		/// <summary>Gets the full name of the page.</summary>
		/// <value>The full name of the page.</value>
		string FullPageName { get; }

		/// <summary>Gets the namespace the page is in.</summary>
		/// <value>The namespace.</value>
		Namespace Namespace { get; }

		/// <summary>Gets the name of the page without the namespace.</summary>
		/// <value>The name of the page without the namespace.</value>
		string PageName { get; }
		#endregion

		/// <summary>Indicates whether the current title is equal to another title based on Namespace and PageName only.</summary>
		/// <param name="other">A title to compare with this one.</param>
		/// <returns><c>true</c> if the current title is equal to the <paramref name="other"/> parameter; otherwise, <c>false</c>.</returns>
		/// <remarks>This method is named as it is to avoid any ambiguity about what is being checked, as well as to avoid the various issues associated with implementing IEquatable on unsealed types.</remarks>
		bool SimpleEquals(ISimpleTitle other);
	}
}