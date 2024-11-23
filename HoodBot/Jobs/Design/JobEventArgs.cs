namespace RobinHood70.HoodBot.Jobs.Design;

using System;
using RobinHood70.HoodBot.Models;

public class JobEventArgs(JobInfo job, Exception? e) : EventArgs
{
	#region Public Properties
	public bool Abort { get; set; } = e != null; // Default is to abort on exception (whether cancellation or true error), but subscribers can change this.

	public bool Cancelled => this.Exception is OperationCanceledException;

	public Exception? Exception { get; } = e;

	public JobInfo Job { get; } = job;

	public bool Success => this.Exception == null;
	#endregion
}