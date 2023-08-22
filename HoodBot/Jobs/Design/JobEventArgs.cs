namespace RobinHood70.HoodBot.Jobs.Design
{
	using System;
	using RobinHood70.HoodBot.Models;

	public class JobEventArgs : EventArgs
	{
		#region Constructors
		public JobEventArgs(JobInfo job, Exception? e)
		{
			this.Job = job;
			this.Exception = e;
			this.Abort = e != null; // Default is to abort on exception (whether cancellation or true error), but subscribers can change this.
		}
		#endregion

		#region Public Properties
		public bool Abort { get; set; }

		public bool Cancelled => this.Exception is OperationCanceledException;

		public Exception? Exception { get; }

		public JobInfo Job { get; }

		public bool Success => this.Exception == null;
		#endregion
	}
}