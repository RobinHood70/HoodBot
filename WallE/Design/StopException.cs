namespace RobinHood70.WallE.Design;

using System;
using RobinHood70.CommonCode;
using RobinHood70.WallE.Properties;

/// <summary>The exception that is thrown when a stop has been requested via the wiki.</summary>
/// <remarks>Typical sources of this exception are a change to the logged-in user's talk page, the user changing or being logged out unexpectedly, or via a custom stop check method.</remarks>
/// <seealso cref="Exception" />
/// <remarks>Initializes a new instance of the <see cref="StopException" /> class.</remarks>
/// <param name="message">The error message that explains the reason for the exception.</param>
/// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
public class StopException(string message, Exception? innerException) : Exception(Globals.CurrentCulture(Messages.StopRequested, message), innerException)
{
	#region Constructors

	/// <summary>Initializes a new instance of the <see cref="StopException" /> class.</summary>
	public StopException()
		: this(string.Empty, null)
	{
	}

	/// <summary>Initializes a new instance of the <see cref="StopException" /> class.</summary>
	/// <param name="message">The message that describes the error.</param>
	public StopException(string message)
		: this(message, null)
	{
	}
	#endregion
}