namespace RobinHood70.HoodBot.Uesp
{
	using System.Collections.Generic;
	using RobinHood70.HoodBot.Models;
	using RobinHood70.Robby;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Eve;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	public class UespSite : Site, IResultPageHandler, IJobAware, IJobLogger
	{
		#region Constructors
		public UespSite(IWikiAbstractionLayer abstractionLayer)
			: base(abstractionLayer)
		{
			if (abstractionLayer is WikiAbstractionLayer eve)
			{
				var moduleFactory = eve.ModuleFactory;
				moduleFactory.RegisterProperty<VariablesInput>(PropVariables.CreateInstance);
				moduleFactory.RegisterGenerator<VariablesInput>(PropVariables.CreateInstance);
				eve.Assert = "bot";
				eve.StopCheckMethods = StopCheckMethods.Assert | StopCheckMethods.TalkCheckNonQuery | StopCheckMethods.TalkCheckQuery;
				eve.UserCheckFrequency = 10;
			}
		}
		#endregion

		#region Public Properties
		public JobLogger? JobLogger { get; private set; }

		public Page? LogPage { get; private set; }

		public PageResultHandler? ResultPageHandler { get; private set; }
		#endregion

		#region Public Static Methods
		public static UespSite CreateInstance(IWikiAbstractionLayer abstractionLayer) => new UespSite(abstractionLayer);
		#endregion

		#region Public Override Methods
		public void OnJobsCompleted(bool success)
		{
			if (this.ResultPageHandler != null)
			{
				this.ResultPageHandler.Save();
				this.ResultPageHandler.Clear();
			}
		}

		public void OnJobsStarted()
		{
			this.FilterPages.Clear();
			this.FilterPages.Add(new Title(this, MediaWikiNamespaces.Project, "Bot Requests"));
		}
		#endregion

		#region Protected Override Methods
		protected override IReadOnlyCollection<Title> LoadDeletionTemplates() => new TitleCollection(this, MediaWikiNamespaces.Template, "Proposeddeletion", "Prod", "Speed", "Speedydeletion");

		protected override IReadOnlyCollection<Title> LoadDeletePreventionTemplates() => new TitleCollection(this, MediaWikiNamespaces.Template, "Empty category", "Linked image");

		protected override IReadOnlyCollection<Title> LoadDiscussionPages()
		{
			var titles = new TitleCollection(this);
			titles.GetCategoryMembers("Message Boards");
			return titles;
		}

		protected override void Login(LoginInput input)
		{
			base.Login(input);

			ThrowNull(this.User, nameof(UespSite), nameof(this.User));
			this.ClearMessage(true);

			var resultPage = new Title(this, this.User.FullPageName + "/Results");
			this.ResultPageHandler = new PageResultHandler(resultPage);
			this.FilterPages.Add(resultPage);

			this.LogPage = new Page(this, this.User.FullPageName + "/Log");
			this.FilterPages.Add(this.LogPage);
			this.JobLogger = new PageJobLogger(JobTypes.Write, this.LogPage);
			//// Reinstate if pages become different: this.FilterPages.Add(this.StatusPage);
		}
		#endregion
	}
}
