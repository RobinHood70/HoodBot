﻿namespace RobinHood70.WallE.Design;

using System;
using System.ComponentModel;
using RobinHood70.CommonCode;
using RobinHood70.WallE.Properties;

/// <summary>The exception thrown when the wiki returns an error instead of the expected result.</summary>
/// <seealso cref="Exception" />
public class WikiException : Exception
{
	#region Constructors

	/// <summary>Initializes a new instance of the <see cref="WikiException" /> class.</summary>
	public WikiException()
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
	#endregion

	#region Public Properties

	/// <summary>Gets the error code.</summary>
	/// <value>The error code.</value>
	public string? Code { get; }

	/// <summary>Gets the descriptive error information.</summary>
	/// <value>The descriptive error information.</value>
	public string? Info { get; }
	#endregion

	#region Public Static Method

	/// <summary>Static constructor for a generalized WikiException.</summary>
	/// <param name="code">The error's <c>code</c> value.</param>
	/// <param name="info">The error's <c>info</c> value.</param>
	/// <returns>A new WikiException instance with a general error message.</returns>
	public static WikiException General(string code, [Localizable(true)] string info) => new(Globals.CurrentCulture(Messages.WikiExceptionGeneral, code, info), code, info);
	#endregion
}