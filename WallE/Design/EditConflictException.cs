namespace RobinHood70.WallE.Design
{
	using System;

	/// <summary>The exception thrown when there's an edit conflict.</summary>
	/// <seealso cref="Exception" />
	public class EditConflictException : Exception
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="EditConflictException"/> class.</summary>
		public EditConflictException()
		{
		}

		/// <summary>Initializes a new instance of the <see cref="EditConflictException"/> class with a specified error message.</summary>
		/// <param name="message">The message that describes the error.</param>
		public EditConflictException(string message)
			: base(message)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="EditConflictException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.</summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		/// <param name="innerException">The exception that is the cause of the current exception, or a null reference (<see langword="Nothing" /> in Visual Basic) if no inner exception is specified.</param>
		public EditConflictException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
		#endregion
	}
}