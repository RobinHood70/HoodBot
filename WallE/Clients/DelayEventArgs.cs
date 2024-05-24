namespace RobinHood70.WallE.Clients
{
	using System;
	using System.ComponentModel;

	/// <summary>EventArgs used for the RequestingDelay event when the wiki or client requests a specific delay before the next edit attempt.</summary>
	/// <remarks>Initializes a new instance of the <see cref="DelayEventArgs" /> class.</remarks>
	/// <param name="delayTime">How long to delay.</param>
	/// <param name="reason">The reason/source of the delay.</param>
	/// <param name="description">The human-readable description or the reason for the delay.</param>
	public class DelayEventArgs(TimeSpan delayTime, DelayReason reason, string description) : CancelEventArgs
	{
		#region Public Properties

		/// <summary>Gets the delay time.</summary>
		/// <value>The delay time.</value>
		public TimeSpan DelayTime { get; } = delayTime;

		/// <summary>Gets the human-readable description of the delay reason.</summary>
		/// <value>The description.</value>
		public string Description { get; } = description;

		/// <summary>Gets the reason for the delay.</summary>
		/// <value>The reason for the delay.</value>
		public DelayReason Reason { get; } = reason;
		#endregion
	}
}