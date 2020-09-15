namespace RobinHood70.HoodBot.Jobs.Design
{
	using RobinHood70.Robby;
	using static RobinHood70.CommonCode.Globals;

	/// <summary>Implements the <see cref="ResultHandler" /> class and saves results to a wiki page.</summary>
	/// <seealso cref="ResultHandler" />
	public class PageResultHandler : ResultHandler
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="PageResultHandler"/> class.</summary>
		/// <param name="title">A <see cref="Robby.Title"/> that points to the results page.</param>
		public PageResultHandler(Site site, string pageName)
			: base(site?.Culture)
		{
			ThrowNull(site, nameof(site));
			ThrowNull(pageName, nameof(pageName));
			this.Title = Page.FromName(site, pageName);
			this.DefaultText = this.ResourceManager.GetString("Results", site.Culture);
		}
		#endregion

		#region Public Properties
		public Page Title { get; set; }
		#endregion

		#region Public Methods

		/// <inheritdoc/>
		public override void Save()
		{
			if (this.StringBuilder.Length > 0)
			{
				this.Title.Text = this.StringBuilder.ToString();
				this.Title.Save(this.Description, false);
			}
		}
		#endregion
	}
}
