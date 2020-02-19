namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.WikiClasses.Parser;
	using RobinHood70.WikiCommon;

	public class OneOffJob : EditJob
	{
		#region Constructors
		[JobInfo("One-Off Job")]
		public OneOffJob([ValidatedNotNull] Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo) => this.Pages.SetLimitations(LimitationType.Remove, MediaWikiNamespaces.User);
		#endregion

		#region Public Override Properties
		public override string LogName => "One-Off Job";
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			this.Pages.PageLoaded += this.Results_PageLoaded;
			this.Pages.GetBacklinks("Template:Mod Icon Data", BacklinksTypes.EmbeddedIn);
			this.Pages.Sort();
			this.Pages.PageLoaded -= this.Results_PageLoaded;
		}

		protected override void Main() => this.SavePages("Work around mod header issue on save");
		#endregion

		#region Private Methods
		private void Results_PageLoaded(object sender, Page page)
		{
			var parsedText = WikiTextParser.Parse(page.Text);
			var modIconData = parsedText.FindFirstLinked<TemplateNode>(item => item.BacklinkTitleToParts(this.Site).PageName == "Mod Icon Data");
			var modHeader = parsedText.FindFirst<TemplateNode>(item => item.BacklinkTitleToParts(this.Site).PageName == "Mod Header");
			if (modIconData != null && modHeader != null)
			{
				var modHeaderNameRaw = modHeader.FindNumberedParameter(1);
				var modHeaderName = modHeaderNameRaw == null ? null : WikiTextVisitor.Value(modHeaderNameRaw.Value);
				if (modIconData.Value is TemplateNode modIconTemplate && modHeaderName == page.PageName)
				{
					if (modIconData.Previous?.Value is TextNode textNode && textNode.Text[^1] == '\n')
					{
						textNode.Text = textNode.Text[0..^1].TrimEnd(' ');
					}

					parsedText.Remove(modIconData);
					var icon = modIconTemplate.FindNumberedParameter(1);
					if (icon != null)
					{
						icon.SetName("icon");
						modHeader.Parameters.AddLast(icon);
					}

					var iconSize = modIconTemplate.FindNumberedParameter(2);
					if (iconSize != null)
					{
						var sizeValue = WikiTextVisitor.Raw(iconSize.Value);
						if (sizeValue != "32" && sizeValue != "x32")
						{
							iconSize.SetName("iconSize");
							modHeader.Parameters.AddLast(iconSize);
						}
					}
				}

				page.Text = WikiTextVisitor.Raw(parsedText);
			}
		}
		#endregion
	}
}
