namespace RobinHood70.Robby.Design
{
	/// <summary>Represents a simple title with the addition of an immutable key.</summary>
	/// <seealso cref="ISimpleTitle" />
	public interface IKeyedTitle : ISimpleTitle
	{
		/// <summary>Gets the key for the title.</summary>
		/// <value>The key.</value>
		/// <remarks>The key should be immutable—that is, once initialized, it must never change during the lifetime of the object. Implementers should assign a value in the constructor, rather than referencing any mutable properties. In other words, implement this as <c>this.Key = this.FullPageName</c>, for instance, <em>not</em> as <c>public string Key { get; } => this.FullPageName;</c>.</remarks>
		string Key { get; }
	}
}