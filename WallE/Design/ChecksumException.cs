namespace RobinHood70.WallE.Design
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>Exception that is raised in the event of a checksum mismatch.</summary>
	/// <seealso cref="Exception" />
	/// <seealso cref="ISerializable" />
	[Serializable]
	public class ChecksumException : Exception, ISerializable
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="ChecksumException" /> class.</summary>
		public ChecksumException()
			: base()
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

		/// <summary>Initializes a new instance of the <see cref="ChecksumException" /> class.</summary>
		/// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
		/// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
		protected ChecksumException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
		#endregion
	}
}
