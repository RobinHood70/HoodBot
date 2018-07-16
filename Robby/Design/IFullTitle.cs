namespace RobinHood70.Robby.Design
{
	/// <summary>Represents full title information, including interwiki, namespace, page name, and fragment.</summary>
	/// <seealso cref="ISimpleTitle" />
	public interface IFullTitle : ISimpleTitle
	{
		/// <summary>Gets the fragment.</summary>
		/// <value>The fragment.</value>
		string Fragment { get; }

		/// <summary>Gets the interwiki entry.</summary>
		/// <value>The interwiki entry.</value>
		InterwikiEntry Interwiki { get; }

		/// <summary>Indicates whether the current title is equal to another title based on Interwiki, Namespace, PageName, and Fragment.</summary>
		/// <param name="other">A title to compare with this one.</param>
		/// <returns><c>true</c> if the current title is equal to the <paramref name="other"/> parameter; otherwise, <c>false</c>.</returns>
		/// <remarks>This method is named as it is to avoid any ambiguity about what is being checked, as well as to avoid the various issues associated with implementing IEquatable on unsealed types.</remarks>
		bool FullEquals(IFullTitle other);
	}
}