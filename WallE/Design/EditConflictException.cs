namespace RobinHood70.WallE.Design
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>The exception thrown when there's an edit conflict.</summary>
	/// <seealso cref="Exception" />
	/// <seealso cref="ISerializable" />
	[Serializable]
	public class EditConflictException : Exception, ISerializable
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="EditConflictException"/> class.</summary>
		public EditConflictException()
			: base()
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

		/// <summary>Initializes a new instance of the <see cref="EditConflictException"/> class with serialized data.</summary>
		/// <param name="info">The <see cref="SerializationInfo" /> that holds the serialized object data about the exception being thrown. </param>
		/// <param name="context">The <see cref="StreamingContext" /> that contains contextual information about the source or destination. </param>
		/// <exception cref="ArgumentNullException">The <paramref name="info" /> parameter is <see langword="null" />. </exception>
		/// <exception cref="SerializationException">The class name is <see langword="null" /> or <see cref="Exception.HResult" /> is zero (0). </exception>
		protected EditConflictException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
		#endregion
	}
}