namespace RobinHood70.Robby
{
	using System;

	// Require/allow only basic object methods so that clients looking at WarningEventArgs.Sender know that it's intended to be informational only, not to allow interaction with the sending object.
	public interface IMessageSource
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Echoes built-in object.GetType().")]
		Type GetType();

		string ToString();
	}
}
