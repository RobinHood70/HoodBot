namespace RobinHood70.Robby.Design
{
	using System;

	#region Public Delegates

	/// <summary>Represents a method which creates a <see cref="UserFunctions"/> derivative.</summary>
	/// <param name="site">The site the <see cref="UserFunctions"/> object is being created by.</param>
	/// <returns>A <see cref="UserFunctions"/> object.</returns>
	public delegate UserFunctions UserFunctionsFactory(Site site);
	#endregion

	#region Public Enumerations

	/// <summary>The types of jobs to be logged, if logging is available for the curent site/bot.</summary>
	[Flags]
	public enum LogJobTypes
	{
		/// <summary>Don't log anything.</summary>
		None = 0,

		/// <summary>Log read-only jobs.</summary>
		Read = 1,

		/// <summary>Log jobs that change the wiki in any way.</summary>
		Write = 1 << 1,
	}

	/// <summary>Where to save any results sent to the <see cref="UserFunctions.AddResult(ResultDestination, string)"/> methods.</summary>
	public enum ResultDestination
	{
		/// <summary>Do nothing with any results.</summary>
		None,

		/// <summary>Add results to the results page.</summary>
		ResultsPage,

		/// <summary>Add results to a user's talk page.</summary>
		UserTalkPage,

		/// <summary>Add results to the requests page.</summary>
		RequestPage,

		/// <summary>Add results to an e-mail to be sent to the user.</summary>
		Email,

		/// <summary>Add results to a local file.</summary>
		LocalFile,
	}
	#endregion

	/// <summary>Provides user/bot-specific functionality for a given site or sites.</summary>
	public abstract class UserFunctions
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="UserFunctions"/> class.</summary>
		/// <param name="site">The site object associated with this instance.</param>
		protected UserFunctions(Site site) => this.Site = site;
		#endregion

		#region Public Properties

		/// <summary>Gets or sets the default result destination when no destination is specified.</summary>
		/// <value>The default result destination.</value>
		public ResultDestination DefaultResultDestination { get; set; } = ResultDestination.None;

		/// <summary>Gets or sets the user's log page.</summary>
		/// <value>The log page.</value>
		public Page LogPage { get; protected set; }

		/// <summary>Gets or sets the results page.</summary>
		/// <value>The results page.</value>
		public Page ResultsPage { get; protected set; }

		/// <summary>Gets the site associated with this instance.</summary>
		/// <value>The site associated with this instance.</value>
		public Site Site { get; }

		/// <summary>Gets or sets the user's status page.</summary>
		/// <value>The status page.</value>
		public Page StatusPage { get; protected set; }
		#endregion

		#region Public Abstract Properties

		/// <summary>Gets the job types that should be logged.</summary>
		/// <value>The job types to be logged.</value>
		public abstract LogJobTypes LogJobTypes { get; }
		#endregion

		#region Public Virtual Methods

		/// <summary>Adds the result to the default destination.</summary>
		/// <param name="text">The text.</param>
		public void AddResult(string text) => this.AddResult(this.DefaultResultDestination, text);

		/// <summary>Adds the result to the specified destination.</summary>
		/// <param name="destination">The destination.</param>
		/// <param name="text">The text.</param>
		public virtual void AddResult(ResultDestination destination, string text)
		{
		}

		/// <summary>Initializes any data required to publish results to the specified destination.</summary>
		/// <param name="destination">The destination.</param>
		/// <param name="user">The user.</param>
		/// <param name="title">The title.</param>
		public virtual void InitializeResult(ResultDestination destination, string user, string title)
		{
		}

		/// <summary>Called when completing a job run.</summary>
		/// <remarks>This allows for custom actions to occur after a job run. It is left to the client to decide when that has occured and call the method.</remarks>
		public virtual void OnAllJobsComplete()
		{
		}

		/// <summary>Called when starting a job run.</summary>
		/// <param name="jobCount">The number of jobs being run.</param>
		/// <remarks>This allows for custom actions to occur before a job run. It is left to the client to decide when that should occur and call the method.</remarks>
		public virtual void OnAllJobsStarting(int jobCount)
		{
		}

		/// <summary>Sets the result information for the specified destination.</summary>
		/// <param name="destination">The destination.</param>
		/// <param name="user">The user.</param>
		/// <param name="title">The title.</param>
		public virtual void SetResultInfo(ResultDestination destination, string user, string title)
		{
		}

		/// <summary>Sets the result title only, without affecting the user (if any).</summary>
		/// <param name="destination">The destination.</param>
		/// <param name="title">The title.</param>
		public virtual void SetResultTitle(ResultDestination destination, string title)
		{
		}

		/// <summary>Returns a value indicating whether the log entry represented in the <paramref name="info"/> parameter should be logged.</summary>
		/// <param name="info">The log information to be checked.</param>
		/// <returns>A value indicating whether the log entry represented in the <paramref name="info"/> parameter should be logged.</returns>
		public virtual bool ShouldLog(LogInfo info) =>
			info == null ? false :
			((info.ReadOnly && this.LogJobTypes.HasFlag(LogJobTypes.Read)) ||
			(!info.ReadOnly && this.LogJobTypes.HasFlag(LogJobTypes.Write)));
		#endregion

		#region Public Abstract Methods

		/// <summary>Adds a new log entry.</summary>
		/// <param name="info">The log information to be added.</param>
		/// <returns>A <see cref="ChangeStatus"/> value indicating whether the log page was changed.</returns>
		public abstract ChangeStatus AddLogEntry(LogInfo info);

		/// <summary>Performs any site-specific customizations.</summary>
		public abstract void DoSiteCustomizations();

		/// <summary>Ends the log entry.</summary>
		/// <returns>A <see cref="ChangeStatus"/> value indicating whether the log page was changed.</returns>
		/// <remarks>Typically, this will involve indicating that the job is done in some fashion.</remarks>
		public abstract ChangeStatus EndLogEntry();

		/// <summary>Updates the current status on the page found in <see cref="StatusPage"/>.</summary>
		/// <param name="status">The text to use for the status update.</param>
		/// <returns>A <see cref="ChangeStatus"/> value indicating whether the page was updated.</returns>
		public abstract ChangeStatus UpdateCurrentStatus(string status);
		#endregion
	}
}
