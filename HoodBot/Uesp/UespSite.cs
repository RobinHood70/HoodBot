﻿namespace RobinHood70.HoodBot.Uesp
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot;
	using RobinHood70.Robby;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Eve;
	using RobinHood70.WikiCommon;
	using static RobinHood70.CommonCode.Globals;

	public class UespSite : Site, IResultHandler, IJobAware, IJobLogger
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

		public ResultHandler? ResultHandler { get; private set; }
		#endregion

		#region Public Static Methods
		public static UespSite CreateInstance(IWikiAbstractionLayer abstractionLayer) => new UespSite(abstractionLayer);

		public static string GetBotFolder() => Environment.ExpandEnvironmentVariables(@"%BotData%");
		#endregion

		#region Public Methods
		public void OnJobsCompleted(bool success)
		{
			this.FilterPages.Remove("Project:Bot Requests");
			if (this.ResultHandler != null)
			{
				this.ResultHandler.Save();
				this.ResultHandler.Clear();
			}
		}

		public void OnJobsStarted() => this.FilterPages.Add(new Title(this[MediaWikiNamespaces.Project], "Bot Requests"));
		#endregion

		#region Public Override Methods
		public override void Logout(bool force)
		{
			if (this.User != null)
			{
				this.FilterPages.Remove(this.User.FullPageName + "/Results");
			}

			if (this.LogPage != null)
			{
				this.FilterPages.Remove(this.LogPage);
			}

			base.Logout(force);
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

			// Messages have to be cleared in order to get pages from the wiki properly, so force that to happen if there are messages waiting, even if editing is disabled.
			if (this.AbstractionLayer.CurrentUserInfo?.Flags.HasFlag(UserInfoFlags.HasMessage) ?? false)
			{
				this.ClearMessage(true);
			}

			var resultPage = new Title(this[MediaWikiNamespaces.User], this.User.PageName + "/Results");
			this.ResultHandler = new PageResultHandler(resultPage);
			this.FilterPages.Add(resultPage);

			this.LogPage = new Page(this[MediaWikiNamespaces.User], this.User.PageName + "/Log");
			this.FilterPages.Add(this.LogPage);
			this.JobLogger = new PageJobLogger(JobTypes.Write, this.LogPage);
			//// Reinstate if pages become different: this.FilterPages.Add(this.StatusPage);
		}
		#endregion
	}
}