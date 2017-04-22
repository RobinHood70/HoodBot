namespace RobinHood70.WallE.Design
{
	using System;
	using Base;

	/// <summary>Event arguments for MediaWiki warnings.</summary>
	/// <seealso cref="EventArgs" />
	public class WarningEventArgs : EventArgs
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="WarningEventArgs"/> class.</summary>
		/// <param name="warning">The warning.</param>
		public WarningEventArgs(ErrorItem warning) => this.Warning = warning;
		#endregion

		#region Public Properties

		/// <summary>Gets the warning object.</summary>
		/// <value>The warning object.</value>
		public ErrorItem Warning { get; }
		#endregion
	}
}
