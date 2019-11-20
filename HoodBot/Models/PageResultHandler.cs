namespace RobinHood70.HoodBot.Models
{
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Implements the <see cref="ResultHandler" /> class and saves results to a wiki page.</summary>
	/// <seealso cref="ResultHandler" />
	public class PageResultHandler : ResultHandler
	{
		#region Fields
		private readonly ISimpleTitle title;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="PageResultHandler"/> class.</summary>
		/// <param name="title">A <see cref="Title"/> that points to the results page.</param>
		public PageResultHandler(ISimpleTitle title)
			: base(title?.Site.Culture)
		{
			ThrowNull(title, nameof(title));
			this.title = title;
			this.DefaultText = this.ResourceManager.GetString("Results", title.Site.Culture);
		}
		#endregion

		#region Public Methods

		/// <inheritdoc/>
		public override void Save()
		{
			if (this.StringBuilder.Length > 0)
			{
				var page = new Page(this.title) { Text = this.StringBuilder.ToString() };
				page.Save(this.Description, false);
			}
		}
		#endregion
	}
}
