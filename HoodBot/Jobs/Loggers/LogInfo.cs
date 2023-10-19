namespace RobinHood70.HoodBot.Jobs.Loggers
{
	/// <summary>This provides a basic log information class to be derived from and customized as needed by custom JobLogger implementations.</summary>
	public class LogInfo
	{
		/// <summary>Initializes a new instance of the <see cref="LogInfo"/> class.</summary>
		/// <param name="title">The title for the log entry.</param>
		/// <param name="details">The details for the log entry (typically, job parameters or notes).</param>
		public LogInfo(string title, string details)
		{
			this.Title = title;
			this.Details = details;
		}

		/// <summary>Gets any additional details about the log entry.</summary>
		/// <value>The additional details.</value>
		public string Details { get; }

		/// <summary>Gets the log title.</summary>
		/// <value>The title.</value>
		/// <remarks>This value should generally be short. Additional information can be added in <see paramref="Details"/>.</remarks>
		public string Title { get; }
	}
}