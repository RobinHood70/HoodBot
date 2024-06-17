namespace RobinHood70.HoodBot.Uesp
{
	using System;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
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
		#endregion

		#region Public Override Methods
		public override void Logout(bool force)
		{
			if (this.User is User user)
			{
				this.FilterPages.Remove(user.Title.FullPageName() + "/Results");
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
		protected override TitleCollection LoadDeletionCategories() => new(this, MediaWikiNamespaces.Category, "Marked for Deletion");

		protected override TitleCollection LoadDeletePreventionTemplates() => new(this, MediaWikiNamespaces.Template, "Empty category", "Linked image");

		protected override TitleCollection LoadDiscussionPages()
		{
			TitleCollection titles = new(this);
			titles.GetCategoryMembers("Message Boards");
			return titles;
		}

		protected override void Login(LoginInput? input)
		{
			base.Login(input);

			this.User.PropertyThrowNull(nameof(UespSite), nameof(this.User));

			if (this.EditingEnabled)
			{
				// Assumes we'll never be editing UESP anonymously.
				this.AbstractionLayer.Assert = string.Equals(this.User.Title.PageName, "HoodBot", StringComparison.Ordinal)
					? "bot"
					: "user";
			}

			// Messages have to be cleared in order to get pages from the wiki properly, so force that to happen if there are messages waiting, even if editing is disabled.
			if (this.AbstractionLayer.CurrentUserInfo?.Flags.HasAnyFlag(UserInfoFlags.HasMessage) ?? false)
			{
				this.ClearMessage(force: true);
			}

			var resultPage = TitleFactory.FromValidated(this[MediaWikiNamespaces.User], this.User.Title.PageName + "/Results");
			this.FilterPages.Add(resultPage);

			this.LogTitle = TitleFactory.FromValidated(this[MediaWikiNamespaces.User], this.User.Title.PageName + "/Log");
			this.FilterPages.Add(this.LogTitle);
		}

		protected override void ParseInternalSiteInfo()
		{
			base.ParseInternalSiteInfo();
			this.FilterPages.Add(TitleFactory.FromUnvalidated(this[MediaWikiNamespaces.Project], "Bot Requests"));
		}
		#endregion
	}
}