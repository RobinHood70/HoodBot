﻿namespace RobinHood70.Robby;

using System;
using RobinHood70.Robby.Design;

/// <summary>Represents a warning generated by Robby or, by extension, the wiki itself.</summary>
/// <seealso cref="EventArgs" />
/// <remarks>Initializes a new instance of the <see cref="WarningEventArgs"/> class.</remarks>
/// <param name="sender">The sender of the warning.</param>
/// <param name="warning">The warning text.</param>
public class WarningEventArgs(IMessageSource sender, string warning) : EventArgs
{
	#region Public Properties

	/// <summary>Gets the sender of the warning.</summary>
	/// <value>The sender of the warning.</value>
	public IMessageSource Sender { get; } = sender;

	/// <summary>Gets the warning text.</summary>
	/// <value>The warning text.</value>
	public string Warning { get; } = warning;
	#endregion
}