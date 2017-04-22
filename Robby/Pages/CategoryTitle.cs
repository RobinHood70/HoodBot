namespace RobinHood70.Robby.Pages
{
	public class CategoryTitle : Title
	{
		#region Constructors
		internal CategoryTitle(Site site, string fullCategoryName, string sortKey, bool hidden)
			: base(site, fullCategoryName)
		{
			this.Hidden = hidden;
			this.SortKey = sortKey;
		}
		#endregion

		#region Public Properties
		public bool Hidden { get; }

		public string SortKey { get; }
		#endregion
	}
}
