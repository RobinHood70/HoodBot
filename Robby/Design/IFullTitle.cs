namespace RobinHood70.Robby.Design
{
	/// <summary>Represents full title information, including interwiki, namespace, page name, and fragment.</summary>
	/// <seealso cref="Title" />
	public interface IFullTitle : ITitle
	{
		#region Properties

		/// <summary>Gets the fragment.</summary>
		/// <value>The fragment.</value>
		string? Fragment { get; }

		/// <summary>Gets the interwiki entry.</summary>
		/// <value>The interwiki entry.</value>
		InterwikiEntry? Interwiki { get; }
		#endregion
	}
}