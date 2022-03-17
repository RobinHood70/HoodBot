namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.IO;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Uesp;
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
		protected override void BeforeLogging()
		{
			var botFolder = UespSite.GetBotDataFolder(this.fileName);
			var parser = new SiteNodeFactory(this.Site).Parse(File.ReadAllText(botFolder));
			for (var i = 0; i < parser.Count; i += 4)
			{
				CheckText(parser, i + 1);
				CheckText(parser, i + 3);
				IHeaderNode? header = (IHeaderNode)parser[i];
				if (header.Title.Count < 3 || header.Title[1] is not SiteLinkNode link)
				{
					link = (SiteLinkNode)parser.Factory.LinkNodeFromParts("Blades:" + header.Title.ToValue().Trim(TextArrays.EqualsSign));
				}

				ITemplateNode? template = (ITemplateNode)parser[i + 2];
				var text = "{{Minimal}}\n" + WikiTextVisitor.Raw(template) + "\n{{Stub|Item}}";
				var page = this.Site.CreatePage(link.TitleValue, text);
				this.Pages.Add(page);
			}

			static void CheckText(NodeCollection parsed, int offset)
			{
				if (offset < parsed.Count && (parsed[offset] is not ITextNode textNode || textNode.Text.TrimStart().Length != 0))
				{
					throw new InvalidOperationException();
				}
			}
		}

		protected override void Main() => this.SavePages("Create/update item page", false);
		#endregion
	}
}