namespace RobinHood70.WallE.Design;

using System;
using RobinHood70.WallE.Base;

/// <summary>Event arguments for MediaWiki warnings.</summary>
/// <seealso cref="EventArgs" />
/// <remarks>Initializes a new instance of the <see cref="WarningEventArgs" /> class.</remarks>
/// <param name="warning">The warning.</param>
public class WarningEventArgs(ErrorItem warning) : EventArgs
{
	#region Public Properties

	/// <summary>Gets the warning object.</summary>
	/// <value>The warning object.</value>
	public ErrorItem Warning { get; } = warning;
	#endregion
}