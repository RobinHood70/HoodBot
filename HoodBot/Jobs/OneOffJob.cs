namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.IO;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	public class OneOffJob : EditJob
	{
		#region Constructors
		[JobInfo("One-Off Job")]
		public OneOffJob(JobManager jobManager)
			: base(jobManager)
		{
		}

		protected override void BeforeLogging()
		{
			var botFolder = Path.Combine(UespSite.GetBotDataFolder(), "Blades_Generic_Items.txt");
			var parsed = new SiteNodeFactory(this.Site).Parse(File.ReadAllText(botFolder));
			for (var i = 0; i < parsed.Count; i+=4)
			{
				CheckText(parsed, i+1);
				CheckText(parsed, i+3);
				var header = (IHeaderNode)parsed[i];
				var link = (SiteLinkNode)header.Title[1];
				var template = (ITemplateNode)parsed[i + 2];
				var text = "{{Minimal}}\n" + WikiTextVisitor.Raw(template) + "\n{{Stub|Item}}";
				var page = new Page(link.TitleValue)
				{
					Text = text
				};

				this.Pages.Add(page);
			}

			static void CheckText(NodeCollection parsed, int offset)
			{
				if (offset<parsed.Count && (!(parsed[offset] is ITextNode textNode) || textNode.Text.TrimStart().Length != 0))
				{
					throw new InvalidOperationException();
				}
			}
		}

		protected override void Main() => this.SavePages("Create/update item page", false);
		#endregion
	}
}