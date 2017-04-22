namespace RobinHood70.WallE.Clients
{
	using System;
	using System.ComponentModel;

	/// <summary>EventArgs used for the RequestingDelay event when the wiki or client requests a specific delay before the next edit attempt.</summary>
	public class DelayEventArgs : CancelEventArgs
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="DelayEventArgs"/> class.</summary>
		/// <param name="delayTime">How long to delay.</param>
		/// <param name="reason">The reason/source of the delay.</param>
		public DelayEventArgs(TimeSpan delayTime, DelayReason reason)
		{
			this.DelayTime = delayTime;
			this.Reason = reason;
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the delay time.</summary>
		/// <value>The delay time.</value>
		public TimeSpan DelayTime { get; }

		/// <summary>Gets the reason for the delay.</summary>
		/// <value>The reason for the delay.</value>
		public DelayReason Reason { get; }
		#endregion
	}
}
