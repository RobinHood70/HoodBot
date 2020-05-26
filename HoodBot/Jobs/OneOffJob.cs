namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Parser;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;
	using static RobinHood70.CommonCode.Globals;

	public class OneOffJob : ParsedPageJob
	{
		#region Fields
		private readonly Dictionary<string, bool> headers = new Dictionary<string, bool>();
		#endregion

		#region Constructors
		[JobInfo("One-Off Job")]
		public OneOffJob([NotNull, ValidatedNotNull] Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
		}
		#endregion

		#region Protected Override Properties
		protected override string EditSummary => "Remove redundant parameters";
		#endregion

		#region Protected Override Methods
		protected override void LoadPages() => this.Pages.GetBacklinks("Template:Quest Link", BacklinksTypes.EmbeddedIn);

		protected override void ParseText(object sender, ContextualParser parsedPage)
		{
			ThrowNull(parsedPage, nameof(parsedPage));
			foreach (var questLink in parsedPage.FindAll<TemplateNode>(node => node.GetTitleValue() == "Quest Link"))
			{
				if (questLink.FindParameter("ns_base") is ParameterNode nsBase && parsedPage.Title is ISimpleTitle title && title.Namespace.Contains(nsBase.ValueToText() ?? throw new InvalidOperationException()))
				{
					questLink.RemoveParameter("ns_base");
				}

				if (questLink.FindParameter("mod")?.ValueToText() == "CC")
				{
					questLink.RemoveParameter("mod");
				}
			}
		}
		#endregion
	}
}