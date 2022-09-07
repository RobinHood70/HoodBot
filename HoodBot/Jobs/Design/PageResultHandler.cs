namespace RobinHood70.HoodBot.Jobs.Design
{
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;

	/// <summary>Implements the <see cref="ResultHandler" /> class and saves results to a wiki page.</summary>
	/// <seealso cref="ResultHandler" />
	public class PageResultHandler : ResultHandler
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="PageResultHandler"/> class.</summary>
		/// <param name="site">The site the results page is on.</param>
		/// <param name="pageName">The page name to save results to.</param>
		public PageResultHandler(Site site, string pageName)
			: base(site?.Culture)
		{
			site.ThrowNull();
			pageName.ThrowNull();
			this.Page = site.CreatePage(pageName, string.Empty);
			this.DefaultText = this.ResourceManager.GetString("Results", site.Culture);
		}
		#endregion

		#region Public Properties
		public Page Page { get; set; }
		#endregion

		#region Public Methods

		/// <inheritdoc/>
		public override void Save()
		{
			if (this.StringBuilder.Length > 0 &&
				this.StringBuilder.ToString().Trim() is var text &&
				text.Length > 0)
			{
				this.Page.Text = text;
				this.Page.Save(this.Description, false);
			}
		}
		#endregion
	}
}
