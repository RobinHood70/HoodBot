namespace RobinHood70.Robby;

using System;

/// <summary>Exception that is raised in the event of a checksum mismatch.</summary>
/// <seealso cref="Exception" />
public class ChecksumException : Exception
{
	#region Constructors

	/// <summary>Initializes a new instance of the <see cref="ChecksumException" /> class.</summary>
	public ChecksumException()
	{
	}

	/// <summary>Initializes a new instance of the <see cref="ChecksumException" /> class.</summary>
	/// <param name="message">The message that describes the error.</param>
	public ChecksumException(string message)
		: base(message)
	{
	}

	/// <summary>Initializes a new instance of the <see cref="ChecksumException" /> class.</summary>
	/// <param name="message">The error message that explains the reason for the exception.</param>
	/// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
	public ChecksumException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
	#endregion
}