namespace RobinHood70.Robby.Design
{
	public delegate UserFunctions UserFunctionsFactory(Site site);

	public class UserFunctions
	{
		public UserFunctions(Site site)
		{
			this.Site = site;
			this.LogPage = new Page(this.Site, this.User.Page.FullPageName + "/Log");
		}

		/// <summary>Gets the user's log page. By default, this will be the "/Log" subpage of the logged-in user's page.</summary>
		/// <value>The log page.</value>
		public Page LogPage { get; set; }

		public Site Site { get; }

		public User User => this.Site.User;

		public static UserFunctions CreateInstance(Site site) => new UserFunctions(site);

		public virtual void BeginLogEntry()
		{
		}

		public virtual void EndLogEntry()
		{
		}
	}
}
