namespace RobinHood70.Robby.Design
{
	using System;

	public delegate UserFunctions UserFunctionsFactory(Site site);

	/// <summary>The types of jobs to be logged, if logging is available for the curent site/bot.</summary>
	[Flags]
	public enum LogJobTypes
	{
		/// <summary>  Don't log anything.</summary>
		None = 0,

		/// <summary>  Log read-only jobs.</summary>
		Read = 1,

		/// <summary>  Log jobs that change the wiki in any way.</summary>
		Write = 1 << 1,
	}

	public class UserFunctions
	{
		#region Constructors
		public UserFunctions(Site site)
		{
			this.Site = site;
			this.LogPage = new Page(this.Site, this.User.Page.FullPageName + "/Log");
		}
		#endregion

		#region Public Properties
		public LogJobTypes LogJobTypes { get; } = LogJobTypes.None;

		/// <summary>Gets or sets the user's log page. By default, this will be the "/Log" subpage of the logged-in user's page.</summary>
		/// <value>The log page.</value>
		public Page LogPage { get; set; }

		public Site Site { get; }

		public User User => this.Site.User;
		#endregion

		#region Public Static Methods
		public static UserFunctions CreateDefaultInstance(Site site) => new UserFunctions(site);
		#endregion

		#region Public Virtual Methods
		public virtual void AddLogEntry(LogInfo info)
		{
		}

		public virtual void EndLogEntry()
		{
		}

		public virtual bool ShouldLog(LogInfo info) => info == null ? false : (!info.ReadOnly && this.LogJobTypes.HasFlag(LogJobTypes.Write)) || this.LogJobTypes.HasFlag(LogJobTypes.Read);

		public virtual void UpdateCurrentStatus(Page page, string title)
		{
		}

		public virtual void DoSiteCustomizations()
		{
		}
		#endregion
	}
}
