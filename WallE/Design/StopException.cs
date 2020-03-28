namespace RobinHood70.WallE.Design
{
	using System;
	using System.Runtime.Serialization;
	using RobinHood70.WallE.Properties;
	using static RobinHood70.CommonCode.Globals;
	/// <summary>The exception that is thrown when a stop has been requested via the wiki.</summary>
	/// <remarks>Typical sources of this exception are a change to the logged-in user's talk page, the user changing or being logged out unexpectedly, or via a custom stop check method.</remarks>
	/// <seealso cref="Exception" />
	[Serializable]
	public class StopException : Exception
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

		/// <summary>Initializes a new instance of the <see cref="StopException" /> class.</summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		/// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
		public StopException(string message, Exception? innerException)
			: base(CurrentCulture(Messages.StopRequested, message), innerException)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="StopException" /> class.</summary>
		/// <param name="info">The <see cref="SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
		/// <param name="context">The <see cref="StreamingContext" /> that contains contextual information about the source or destination.</param>
		protected StopException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
		#endregion
	}
}
