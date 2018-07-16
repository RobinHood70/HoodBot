namespace RobinHood70.WallE.Design
{
	using System;
	using System.Runtime.Serialization;
	using System.Security.Permissions;
	using static RobinHood70.WallE.Properties.Messages;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>The exception thrown when the wiki returns an error instead of the expected result.</summary>
	/// <seealso cref="Exception" />
	/// <seealso cref="ISerializable" />
	[Serializable]
	public class WikiException : Exception, ISerializable
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="WikiException" /> class.</summary>
		public WikiException()
			: base()
		{
		}

		/// <summary>Initializes a new instance of the <see cref="WikiException" /> class.</summary>
		/// <param name="message">The message that describes the error.</param>
		public WikiException(string message)
			: base(message)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="WikiException" /> class.</summary>
		/// <param name="message">The message.</param>
		/// <param name="code">The error's <c>code</c> value.</param>
		/// <param name="info">The error's <c>info</c> value.</param>
		public WikiException(string message, string code, string info)
			: this(message)
		{
			this.Code = code;
			this.Info = info;
		}

		/// <summary>Initializes a new instance of the <see cref="WikiException" /> class.</summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		/// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
		public WikiException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="WikiException" /> class.</summary>
		/// <param name="info">The <see cref="SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
		/// <param name="context">The <see cref="StreamingContext" /> that contains contextual information about the source or destination.</param>
		protected WikiException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			if (info != null)
			{
				this.Code = info.GetString("ErrorCode");
				this.Info = info.GetString("ErrorInfo");
			}
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the error code.</summary>
		/// <value>The error code.</value>
		public string Code { get; }

		/// <summary>Gets the descriptive error information.</summary>
		/// <value>The descriptive error information.</value>
		public string Info { get; }
		#endregion

		#region Public Static Method

		/// <summary>Static constructor for a generalized WikiException.</summary>
		/// <param name="code">The error's <c>code</c> value.</param>
		/// <param name="info">The error's <c>info</c> value.</param>
		/// <returns>A new WikiException instance with a general error message.</returns>
		public static WikiException General(string code, string info) => new WikiException(CurrentCulture(WikiExceptionGeneral, code, info), code, info);
		#endregion

		#region Public Override Methods

		/// <summary>Sets the <see cref="SerializationInfo" /> with information about the exception.</summary>
		/// <param name="info">The <see cref="SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
		/// <param name="context">The <see cref="StreamingContext" /> that contains contextual information about the source or destination.</param>
		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			ThrowNull(info, nameof(info));
			base.GetObjectData(info, context);
			info.AddValue("ErrorCode", this.Code);
			info.AddValue("ErrorInfo", this.Info);
		}
		#endregion
	}
}