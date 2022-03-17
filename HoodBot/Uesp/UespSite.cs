namespace RobinHood70.HoodBot.Uesp
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.IO;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Eve;
	using RobinHood70.WikiCommon;

	public class UespSite : Site
	{
		#region Constructors
		public UespSite(IWikiAbstractionLayer abstractionLayer)
			: base(abstractionLayer)
		{
			if (abstractionLayer is WikiAbstractionLayer eve)
			{
				eve.ModuleFactory
					.RegisterProperty<VariablesInput>(PropVariables.CreateInstance)
					.RegisterGenerator<VariablesInput>(PropVariables.CreateInstance);
				eve.StopCheckMethods = StopCheckMethods.Assert | StopCheckMethods.TalkCheckNonQuery | StopCheckMethods.TalkCheckQuery;
				eve.UserCheckFrequency = 10;
			}
		}
		#endregion

		#region Public Properties
		public Title? LogTitle { get; private set; }
		#endregion

		#region Public Static Methods
		public static UespSite CreateInstance(IWikiAbstractionLayer abstractionLayer) => new(abstractionLayer);

		public static string GetBotDataFolder() => Environment.ExpandEnvironmentVariables("%BotData%");

		public static string GetBotDataFolder(string file) => Path.Combine(GetBotDataFolder(), file);
		#endregion

		#region Public Override Methods
		public override void Logout(bool force)
		{
			if (this.User is Title user)
			{
				this.FilterPages.Remove(user.FullPageName + "/Results");
			}

			if (this.LogTitle is not null)
			{
				this.FilterPages.Remove(this.LogTitle);
				this.LogTitle = null;
			}

			base.Logout(force);
		}
		#endregion

		#region Protected Override Methods
		protected override IReadOnlyCollection<Title> LoadDeletionCategories() => new TitleCollection(this, MediaWikiNamespaces.Category, "Marked for Deletion");

		protected override IReadOnlyCollection<Title> LoadDeletePreventionTemplates() => new TitleCollection(this, MediaWikiNamespaces.Template, "Empty category", "Linked image");

		protected override IReadOnlyCollection<Title> LoadDiscussionPages()
		{
			TitleCollection titles = new(this);
			titles.GetCategoryMembers("Message Boards");
			return titles;
		}

		protected override void Login([NotNull, ValidatedNotNull] LoginInput input)
		{
			base.Login(input);

			this.User.ThrowNull(nameof(UespSite), nameof(this.User));

			if (this.EditingEnabled)
			{
				// Assumes we'll never be editing UESP anonymously.
				this.AbstractionLayer.Assert = (
					string.Equals(this.User.PageName, "HotnBOThered", StringComparison.Ordinal) ||
					string.Equals(this.User.PageName, "HoodBot", StringComparison.Ordinal))
						? "bot"
						: "user";
			}

			// Messages have to be cleared in order to get pages from the wiki properly, so force that to happen if there are messages waiting, even if editing is disabled.
			if (this.AbstractionLayer.CurrentUserInfo?.Flags.HasFlag(UserInfoFlags.HasMessage) ?? false)
			{
				this.ClearMessage(force: true);
			}

			var resultPage = CreateTitle.FromValidated(this, MediaWikiNamespaces.User, this.User.PageName + "/Results");
			this.FilterPages.Add(resultPage);

			this.LogTitle = CreateTitle.FromValidated(this, MediaWikiNamespaces.User, this.User.PageName + "/Log");
			this.FilterPages.Add(this.LogTitle);
			//// Reinstate if pages become different: this.FilterPages.Add(this.StatusPage);
		}

		protected override void ParseInternalSiteInfo()
		{
			base.ParseInternalSiteInfo();
			this.FilterPages.Add(CreateTitle.FromUnvalidated(this, MediaWikiNamespaces.Project, "Bot Requests"));
		}
		#endregion
	}
}