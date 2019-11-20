namespace RobinHood70.HoodBot.Models
{
	/// <summary>Indicates that the class supports result handling.</summary>
	public interface IResultPageHandler
	{
		/// <summary>Gets the result page handler.</summary>
		/// <value>The result page handler.</value>
		PageResultHandler? ResultPageHandler { get; }
	}
}