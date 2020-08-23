namespace RobinHood70.HoodBot
{
	/// <summary>Indicates that the class supports result handling.</summary>
	public interface IResultHandler
	{
		/// <summary>Gets the result handler.</summary>
		/// <value>The result handler.</value>
		ResultHandler? ResultHandler { get; }
	}
}