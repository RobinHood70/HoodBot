namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Diagnostics;
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
		public override string LogName => "Update Navboxes";
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			this.Pages.PageLoaded += this.Results_PageLoaded;
			this.Pages.GetCategoryMembers("Navbox Templates");
			this.Pages.Sort();
			foreach (var page in this.Pages)
			{
				Debug.WriteLine(page.FullPageName);
			}

			this.Pages.PageLoaded -= this.Results_PageLoaded;
		}

		protected override void Main() => this.SavePages("Remove name parameter");
		#endregion

		#region Private Methods
		private void Results_PageLoaded(object sender, Page page)
		{
			var parsedText = WikiTextParser.Parse(page.Text);
			var navboxes = parsedText.FindAll<TemplateNode>(item => item.BacklinkTitleToParts(this.Site).PageName == "Navbox");
			foreach (var navbox in navboxes)
			{
				navbox.RemoveParameter("name");
				var navbars = navbox.Parameters.FindAllLinked<ParameterNode>(parameter => parameter.NameToText() == "name");
				foreach (var navbar in navbars)
				{
					var navbarText = WikiTextVisitor.Raw(navbar.Value);
					if (navbar.Previous?.Value is ParameterNode paramNode && navbarText.Contains("\n\n", StringComparison.Ordinal))
					{
						paramNode.Value.AddText("\n");
					}

					navbox.Parameters.Remove(navbar);
				}
			}

			page.Text = WikiTextVisitor.Raw(parsedText);
		}
		#endregion
	}
}
