namespace RobinHood70.HoodBot.Models
{
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using static RobinHood70.CommonCode.Globals;

	/// <summary>Implements the <see cref="ResultHandler" /> class and saves results to a wiki page.</summary>
	/// <seealso cref="ResultHandler" />
	public class PageResultHandler : ResultHandler
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="PageResultHandler"/> class.</summary>
		/// <param name="title">A <see cref="Robby.Title"/> that points to the results page.</param>
		public PageResultHandler(ISimpleTitle title)
			: base(title?.Namespace.Site.Culture)
		{
			ThrowNull(title, nameof(title));
			this.Title = title;
			this.DefaultText = this.ResourceManager.GetString("Results", title.Namespace.Site.Culture);
		}
		#endregion

		#region Public Properties
		public ISimpleTitle Title { get; set; }
		#endregion

		#region Public Methods

		/// <inheritdoc/>
		public override void Save()
		{
			if (this.StringBuilder.Length > 0)
			{
				var page = new Page(this.Title) { Text = this.StringBuilder.ToString() };
				page.Save(this.Description, false);
			}
		}
		#endregion
	}
}
