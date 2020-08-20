namespace RobinHood70.Robby.Design
{
	/// <summary>Represents a simple title with the addition of an immutable key.</summary>
	/// <seealso cref="ISimpleTitle" />
	public interface IKeyedTitle : ISimpleTitle
	{
		/// <summary>Gets the key for the title.</summary>
		/// <value>The key.</value>
		ISimpleTitle Key { get; }
	}
}