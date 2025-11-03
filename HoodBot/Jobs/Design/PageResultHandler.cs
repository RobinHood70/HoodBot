namespace RobinHood70.HoodBot.Jobs.Design;

using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;

/// <summary>Implements the <see cref="ResultHandler" /> class and saves results to a wiki page.</summary>
/// <seealso cref="ResultHandler" />
public class PageResultHandler : ResultHandler
{
	#region Fields
	private string? subPage;
	private Title title;
	#endregion

	#region Constructors

	/// <summary>Initializes a new instance of the <see cref="PageResultHandler"/> class.</summary>
	/// <param name="title">The title of the results page.</param>
	public PageResultHandler(Title title)
	{
		this.title = title;
		this.DefaultText = this.ResourceManager.GetString("Results", title.Site.Culture);
	}
	#endregion

	#region Public Properties
	public bool SaveAsBot { get; set; } = true;

	public string? SubPage
	{
		get => this.subPage;
		set
		{
			this.Save();
			this.subPage = value?.Trim().Trim('/');
		}
	}

	public Title Title
	{
		get => this.title;
		set
		{
			this.Save();
			this.title = value;
		}
	}
	#endregion

	#region Public Methods

	/// <inheritdoc/>
	public override void Save()
	{
		if (this.StringBuilder.Length > 0 &&
			this.StringBuilder.ToString().Trim() is var text &&
			text.Length > 0)
		{
			var fullTitle = string.IsNullOrWhiteSpace(this.SubPage)
				? this.Title
				: TitleFactory.FromValidated(this.Title.Site, this.Title + "/" + this.SubPage);
			var page = Page.FromTitle(fullTitle, text);
			page.Save(this.Description, false, Tristate.Unknown, true, this.SaveAsBot);
		}
	}
	#endregion
}