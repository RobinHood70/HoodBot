namespace RobinHood70.HoodBot.Jobs.Design;

using RobinHood70.CommonCode;
using RobinHood70.Robby;

/// <summary>Implements the <see cref="ResultHandler" /> class and saves results to a wiki page.</summary>
/// <seealso cref="ResultHandler" />
public class PageResultHandler : ResultHandler
{
	#region Fields
	private readonly bool saveAsBot;
	private readonly Title title;
	#endregion

	#region Constructors

	/// <summary>Initializes a new instance of the <see cref="PageResultHandler"/> class.</summary>
	/// <param name="title">The title of the results page.</param>
	/// <param name="saveAsBot">Whether the edit should be flagged as a bot edit when the results are saved.</param>
	public PageResultHandler(Title title, bool saveAsBot)
	{
		this.title = title;
		this.saveAsBot = saveAsBot;
		this.DefaultText = this.ResourceManager.GetString("Results", title.Site.Culture);
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
			var page = Page.FromTitle(this.title, text);
			page.Save(this.Description, false, Tristate.Unknown, true, this.saveAsBot);
		}
	}
	#endregion
}