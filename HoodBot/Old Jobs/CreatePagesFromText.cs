namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.IO;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	public class CreatePagesFromText : EditJob
	{
		#region Fields
		private readonly string fileName;
		#endregion

		#region Constructors
		[JobInfo("Create Pages From Text")]
		public CreatePagesFromText(JobManager jobManager)
			: base(jobManager)
		{
			this.fileName = "Divine_List.txt";
		}
		#endregion

		#region Protected Override Methods

		protected override Action<EditJob, Page>? EditConflictAction => null;

		protected override string EditSummary => "Create/update item page";

		protected override bool MinorEdit => false;
		#endregion

		#region Protected Override Methods
		protected override void LoadPages()
		{
			var botFolder = UespSite.GetBotDataFolder(this.fileName);
			var parser = new WikiNodeFactory().Parse(File.ReadAllText(botFolder));
			for (var i = 0; i < parser.Count; i += 4)
			{
				CheckText(parser, i + 1);
				CheckText(parser, i + 3);
				var header = (IHeaderNode)parser[i];
				if (header.Title.Count < 3 || header.Title[1] is not ILinkNode link)
				{
					link = (ILinkNode)parser.Factory.LinkNodeFromParts("Blades:" + header.GetTitle(true));
				}

				var template = (ITemplateNode)parser[i + 2];
				var text = "{{Minimal}}\n" + WikiTextVisitor.Raw(template) + "\n{{Stub|Item}}";
				var page = this.Site.CreatePage(link.TitleValue, text);
				this.Pages.Add(page);
			}

			static void CheckText(WikiNodeCollection parsed, int offset)
			{
				if (offset < parsed.Count && (parsed[offset] is not ITextNode textNode || textNode.Text.TrimStart().Length != 0))
				{
					throw new InvalidOperationException();
				}
			}
		}
		#endregion
	}
}