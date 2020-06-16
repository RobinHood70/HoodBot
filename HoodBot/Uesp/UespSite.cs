namespace RobinHood70.HoodBot.Uesp
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Models;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Eve;
	using RobinHood70.WikiCommon;
	using static RobinHood70.CommonCode.Globals;

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

		public static string GetBotFolder() => Environment.ExpandEnvironmentVariables(@"%BotData%");
		#endregion

		#region Public Methods
		public void OnJobsCompleted(bool success)
		{
			this.FilterPages.Remove("Project:Bot Requests");
			if (this.ResultPageHandler != null)
			{
				this.ResultPageHandler.Save();
				this.ResultPageHandler.Clear();
			}
		}

		public void OnJobsStarted() => this.FilterPages.Add(new Title(this, "Project:Bot Requests"));
		#endregion

		#region Public Override Methods
		public override void Logout()
		{
			if (this.User != null)
			{
				this.FilterPages.Remove(this.User.FullPageName() + "/Results");
			}

			if (this.LogPage != null)
			{
				this.FilterPages.Remove(this.LogPage);
			}

			base.Logout();
		}
		#endregion

		#region Protected Override Methods
		protected override IReadOnlyCollection<Title> LoadDeletionCategories() => new TitleCollection(this, MediaWikiNamespaces.Category, "Marked for Deletion");

		protected override IReadOnlyCollection<Title> LoadDeletePreventionTemplates() => new TitleCollection(this, MediaWikiNamespaces.Template, "Empty category", "Linked image");

		protected override IReadOnlyCollection<Title> LoadDiscussionPages()
		{
			var titles = new TitleCollection(this);
			titles.GetCategoryMembers("Message Boards");
			return titles;
		}

		protected override void Login([NotNull, ValidatedNotNull] LoginInput input)
		{
			base.Login(input);

			ThrowNull(this.User, nameof(UespSite), nameof(this.User));

			if (this.EditingEnabled)
			{
				// Assumes we'll never be editing UESP anonymously.
				this.AbstractionLayer.Assert = (this.User.Name == "HotnBOThered" || this.User.Name == "HoodBot") ? "bot" : "user";
			}

			// Messages have to be cleared in order to get pages from the wiki properly, so force that to happen, even if editing is disabled.
			this.ClearMessage(true);

			var resultPage = new Title(this, this.User.FullPageName() + "/Results");
			this.ResultPageHandler = new PageResultHandler(resultPage);
			this.FilterPages.Add(resultPage);

			this.LogPage = new Page(this, this.User.FullPageName() + "/Log");
			this.FilterPages.Add(this.LogPage);
			this.JobLogger = new PageJobLogger(JobTypes.Write, this.LogPage);
			//// Reinstate if pages become different: this.FilterPages.Add(this.StatusPage);
		}
		#endregion
	}
}