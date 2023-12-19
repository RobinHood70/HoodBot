namespace RobinHood70.Robby.Design
{
	/// <summary>Represents link information, including whether the link was coerced and initial colons that affect the interpretation of the interwiki and namespace text. Does not attempt to track spacing.</summary>
	/// <seealso cref="Title" />
	public interface ILinkTitle : IFullTitle
	{
		/// <summary>Gets a value indicating whether the title was coerced into its namespace (e.g., {{Example}} is in Template space).</summary>
		/// <value><see langword="true"/> if coerced into the indicated namespace; otherwise, <see langword="false"/>.</value>
		bool Coerced { get; }

		/// <summary>Gets a value indicating whether there was a colon before the interwiki text, thus forcing it to Main space.</summary>
		bool ForcedInterwikiLink { get; }

		/// <summary>Gets a value indicating whether there was a leading colon with the namespace, thus forcing it to Main space.</summary>
		bool ForcedNamespaceLink { get; }
	}
}