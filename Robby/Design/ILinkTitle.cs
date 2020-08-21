namespace RobinHood70.Robby.Design
{
	/// <summary>Represents full title information, including interwiki, namespace, page name, and fragment.</summary>
	/// <seealso cref="ISimpleTitle" />
	public interface ILinkTitle : ISimpleTitle
	{
		/// <summary>Gets the fragment.</summary>
		/// <value>The fragment.</value>
		string? Fragment { get; }

		/// <summary>Gets the interwiki entry.</summary>
		/// <value>The interwiki entry.</value>
		InterwikiEntry? Interwiki { get; }
	}
}