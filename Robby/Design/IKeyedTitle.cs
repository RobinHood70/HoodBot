namespace RobinHood70.Robby.Design
{
	/// <summary>Represents a simple title with the addition of an immutable key.</summary>
	/// <seealso cref="RobinHood70.Robby.Design.ISimpleTitle" />
	public interface IKeyedTitle : ISimpleTitle
	{
		/// <summary>Gets the key for the title.</summary>
		/// <value>The key.</value>
		/// <remarks>The key should be immutable—that is, once initialized, it must never change during the lifetime of the object. Implementers should assign a value in the constructor, rather than referencing any mutable properties. In other words, implement this as <c>this.Key = this.FullPageName</c>, for instance, <em>not</em> as <c>public string Key { get; } => this.FullPageName;</c>.</remarks>
		string Key { get; }

		/// <summary>Indicates whether the current title is equal to another title based on Namespace, PageName, and Key.</summary>
		/// <param name="other">A title to compare with this one.</param>
		/// <returns><c>true</c> if the current title is equal to the <paramref name="other"/> parameter; otherwise, <c>false</c>.</returns>
		/// <remarks>This method is named as it is to avoid any ambiguity about what is being checked, as well as to avoid the various issues associated with implementing IEquatable on unsealed types.</remarks>
		bool KeyedEquals(IKeyedTitle other);
	}
}