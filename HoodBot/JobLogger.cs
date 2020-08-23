namespace RobinHood70.HoodBot
{
	using RobinHood70.HoodBot.Models;

	public abstract class JobLogger
	{
		#region Constructors
		protected JobLogger(JobTypes typesToLog) => this.JobTypes = typesToLog;
		#endregion

		#region Public Properties

		/// <summary>Gets the job types that should be logged.</summary>
		/// <value>The job types to be logged.</value>
		public JobTypes JobTypes { get; }
		#endregion

		#region Public Methods

		/// <summary>Returns a value indicating whether any of the job types in the parameter should be logged.</summary>
		/// <param name="jobType">The log information to be checked.</param>
		/// <returns>A value indicating whether the log entry represented in the <paramref name="info"/> parameter should be logged.</returns>
		public bool ShouldLog(JobTypes jobType) => (this.JobTypes & jobType) != 0;
		#endregion

		#region Public Abstract Methods

		/// <summary>Adds a new entry to the log.</summary>
		/// <param name="info">The log information.</param>
		public abstract void AddLogEntry(LogInfo info);
		#endregion

		#region Public Virtual Methods

		/// <summary>Ends the log entry.</summary>
		/// <remarks>If necessary, log information can be updated at the end of the job.</remarks>
		public virtual void EndLogEntry()
		{
		}
		#endregion
	}
}