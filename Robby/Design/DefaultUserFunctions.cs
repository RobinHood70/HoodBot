namespace RobinHood70.Robby.Design
{
	using System;
	using System.Collections.Generic;

	/// <summary>Represents a default derivative of <see cref="UserFunctions"/> which does nothing.</summary>
	public sealed class DefaultUserFunctions : UserFunctions
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="DefaultUserFunctions"/> class.</summary>
		/// <param name="site">The site object associated with this instance.</param>
		/// <remarks><see cref="DefaultUserFunctions"/> sets <see cref="UserFunctions.LogPage"/> and <see cref="UserFunctions.StatusPage"/> to null.</remarks>
		public DefaultUserFunctions(Site site)
			: base(site)
		{
			this.LogPage = null;
			this.StatusPage = null;
		}
		#endregion

		#region Public Properties

		/// <summary>Gets a list of templates indicating a page is flagged for deletion.</summary>
		/// <value>A list of templates indicating a page is flagged for deletion.</value>
		public override IReadOnlyList<string> DeleteTemplates => Array.Empty<string>();

		/// <summary>Gets a list of templates indicating a page should never be flagged for deletion.</summary>
		/// <value>A list of templates indicating a page should never be flagged for deletion.</value>
		public override IReadOnlyList<string> DoNotDeleteTemplates => Array.Empty<string>();

		/// <summary>Gets the job types that should be logged.</summary>
		/// <value>The job types to be logged.</value>
		/// <remarks><see cref="DefaultUserFunctions"/> always returns <see cref="LogJobTypes.None"/>.</remarks>
		public override LogJobTypes LogJobTypes => LogJobTypes.None;
		#endregion

		#region Public Static Methods

		/// <summary>Creates an instance of the <see cref="DefaultUserFunctions"/> class.</summary>
		/// <param name="site">The site to be associated with the new instance.</param>
		/// <returns>A new instance of the <see cref="DefaultUserFunctions"/> class.</returns>
		public static UserFunctions CreateInstance(Site site) => new DefaultUserFunctions(site);
		#endregion

		#region Public Override Methods

		/// <summary>Adds a new log entry.</summary>
		/// <param name="info">The log information to be added.</param>
		/// <returns>A <see cref="ChangeStatus"/> value indicating whether the log page was changed.</returns>
		/// <remarks><see cref="DefaultUserFunctions"/> always returns <see cref="ChangeStatus.NoEffect"/>.</remarks>
		public override ChangeStatus AddLogEntry(LogInfo info) => ChangeStatus.NoEffect;

		/// <summary>Performs any site-specific customizations.</summary>
		/// <remarks>This is a null function for <see cref="DefaultUserFunctions"/>.</remarks>
		public override void DoSiteCustomizations()
		{
		}

		/// <summary>Ends the log entry.</summary>
		/// <returns>A <see cref="ChangeStatus"/> value indicating whether the log page was changed.</returns>
		/// <remarks>Typically, this will involve indicating that the job is done in some fashion.</remarks>
		/// <remarks><see cref="DefaultUserFunctions"/> always returns <see cref="ChangeStatus.NoEffect"/>.</remarks>
		public override ChangeStatus EndLogEntry() => ChangeStatus.NoEffect;

		/// <summary>Returns a value indicating whether the log entry represented in the <paramref name="info" /> parameter should be logged.</summary>
		/// <param name="info">The log information to be checked.</param>
		/// <returns>A value indicating whether the log entry represented in the <paramref name="info" /> parameter should be logged.</returns>
		/// <remarks><see cref="DefaultUserFunctions"/> always returns <c>false</c>.</remarks>
		public override bool ShouldLog(LogInfo info) => false;

		/// <summary>Updates the current status on the page found in <see cref="UserFunctions.StatusPage"/>.</summary>
		/// <param name="status">The text to use for the status update.</param>
		/// <returns>A <see cref="ChangeStatus"/> value indicating whether the page was updated.</returns>
		/// <remarks><see cref="DefaultUserFunctions"/> always returns <see cref="ChangeStatus.NoEffect"/>.</remarks>
		public override ChangeStatus UpdateCurrentStatus(string status) => ChangeStatus.NoEffect;
		#endregion
	}
}
