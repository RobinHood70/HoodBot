namespace RobinHood70.HoodBot.Models
{
	/// <summary>This provides a basic log information class to be derived from and customized as needed by custom UserFunctions implementations.</summary>
	public class LogInfo
	{
		/// <summary>Initializes a new instance of the <see cref="LogInfo"/> class.</summary>
		/// <param name="title">The title for the log entry.</param>
		/// <param name="details">The details for the log entry (typically, job parameters or notes).</param>
		/// <param name="readOnly">If set to <see langword="true"/>, indicates that the job is read-only.</param>
		public LogInfo(string title, string? details, JobTypes jobType)
		{
			this.Title = title;
			this.Details = details;
			this.JobType = jobType;
		}

		/// <summary>Gets any additional details about the log entry.</summary>
		/// <value>The additional details.</value>
		public string? Details { get; }

		/// <summary>Gets a value indicating the attributes of the job.</summary>
		/// <value>The .</value>
		public JobTypes JobType { get; }

		/// <summary>Gets the log title.</summary>
		/// <value>The title.</value>
		/// <remarks>This value should generally be short. Additional information can be added in <see paramref="Details"/>.</remarks>
		public string Title { get; }
	}
}
