namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	[method: JobInfo("List Similar Images", "Legends")]
	internal sealed class LegendsSimilarList(JobManager jobManager) : TemplateJob(jobManager)
	{
		#region Fields
		private int pageNumber;
		private int resultCount;
		#endregion

		#region Public Properties
		public override string LogName => "Legends Similar Images";
		#endregion

		#region Protected Override Properties
		protected override string TemplateName => "Legends Card Summary";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Add \"Card art\" to unlabeled gallery images";

		protected override void ParseTemplate(SiteTemplateNode template, SiteParser parser)
		{
			if (this.resultCount == 0 && this.Results is PageResultHandler pageResults)
			{
				this.pageNumber++;
				var title = pageResults.Title;
				pageResults.Title = TitleFactory.FromValidated(this.Site[MediaWikiNamespaces.User], $"HoodBot/Legends Images {this.pageNumber}");
				pageResults.SaveAsBot = false;
				this.WriteLine("{| class=\"wikitable compress\"");
			}

			var galleryEntries = new List<SiteLink>();
			if (template.GetValue("image") is string image)
			{
				image = TitleFactory.FromUnvalidated(this.Site[MediaWikiNamespaces.File], image).FullPageName();
				image = $"[[{image}|upright=0.5|thumb|Main Image]]";
			}
			else
			{
				image = "''No Main Image''";
			}

			var factory = (SiteNodeFactory)parser.Factory;
			var galleryNodes = new List<ITagNode>(parser.FindAll<ITagNode>(tag => tag.Name.OrdinalEquals("gallery")));
			foreach (var galleryNode in galleryNodes)
			{
				if (galleryNode.InnerText is not null)
				{
					var ns = factory.Site[MediaWikiNamespaces.File];
					var lines = galleryNode.InnerText.Split(TextArrays.LineFeed);
					var newLines = new List<string>();
					foreach (var line in lines)
					{
						var newLine = line;
						if (line.Trim() is var trimmedLine && trimmedLine.Length > 0)
						{
							var linkNode = factory.LinkNodeFromWikiText("[[" + trimmedLine + " ]]");
							var siteLink = SiteLink.FromLinkNode(ns, linkNode);
							if (string.IsNullOrWhiteSpace(siteLink.Text))
							{
								newLine += "|Card art";
							}
						}

						newLines.Add(newLine);
					}

					galleryNode.InnerText = string.Join('\n', newLines);
				}

				var siteLinks = SiteLink.FromGalleryNode(factory, galleryNode);
				foreach (var link in siteLinks)
				{
					galleryEntries.Add(link);
				}
			}

			this.WriteLine("|-");
			this.WriteLine("| " + image);
			foreach (var entry in galleryEntries)
			{
				this.WriteLine($"| [[{entry.Title.FullPageName()}|upright=0.5|thumb|{entry.Text ?? "''No description''"}]]");
			}

			this.resultCount++;
			if (this.resultCount == 500 && this.Results != null)
			{
				this.WriteLine("|}");
				this.resultCount = 0;
				this.Results.Save();
				this.Results.Clear();
			}
		}
		#endregion
	}
}