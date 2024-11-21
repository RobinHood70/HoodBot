namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Text;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	internal sealed class SFFixedLocations : EditJob
	{
		#region Constants
		private const string SectionName = "Locations";
		#endregion

		#region Fields
		private readonly Dictionary<string, List<RefUse>> refUses = new(StringComparer.Ordinal);
		private readonly TitleCollection footerTemplates;
		#endregion

		#region Constructors
		[JobInfo("Add Locations Section", "Starfield")]
		public SFFixedLocations(JobManager jobManager)
			: base(jobManager)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			this.footerTemplates = new TitleCollection(this.Site)
			{
				new Title(this.Site[MediaWikiNamespaces.Template], "Stub")
			};

			var sfSite = (UespSite)jobManager.Site;
			this.Pages = sfSite.CreateMetaPageCollection(PageModules.Default, false, "editorid", "objectid");
		}
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Add locations section";

		protected override void LoadPages()
		{
			this.footerTemplates.GetCategoryMembers("Navbox Templates");
			this.GetRefUses();
			this.Pages.GetCustomGenerator(new VariablesInput() { Variables = ["editorid", "objectid"] });
		}

		protected override void PageLoaded(Page page)
		{
			var parser = new SiteParser(page);
			var sections = parser.ToSections(2);
			if (sections.FindFirst(SectionName) is not null)
			{
				return;
			}

			if (sections.Count == 1)
			{
				this.CreateFooterSection(sections);
			}

			var metaPage = (VariablesPage)page;
			var editorId = metaPage.GetVariable("editorid");
			var objectId = metaPage.GetVariable("objectid");
			var locations =
				(editorId is not null && this.refUses.TryGetValue(editorId, out var refEditorId)) ? refEditorId :
				(objectId is not null && this.refUses.TryGetValue(objectId, out var refObjectId)) ? refObjectId :
				null;

			if (locations is null || locations.Count == 0)
			{
				return;
			}

			var sb = new StringBuilder(locations.Count * 20);
			sb
				.Append("{| class=\"wikitable\"\n")
				.Append("! Location !! Count\n");
			foreach (var location in locations)
			{
				sb
					.Append("|-\n")
					.Append("| {{Place Link|")
					.Append(location.FullName)
					.Append("|nodesc=1}} || ")
					.Append(location.Count)
					.Append('\n');
			}

			sb.Append("|}\n\n");

			var introContent = sections[0].Content.ToRaw().TrimEnd();
			if (!introContent.EndsWith("{{NewLine}}", StringComparison.Ordinal))
			{
				introContent += "\n\n{{NewLine}}";
			}

			if (!introContent.EndsWith('\n'))
			{
				introContent += '\n';
			}

			sections[0].Content.Clear();
			sections[0].Content.AddText(introContent);

			var locationSection = Section.FromText(parser.Factory, SectionName, sb.ToString());
			sections.Insert(1, locationSection);
			parser.FromSections(sections);
			parser.UpdatePage();
		}

		private void CreateFooterSection(SectionCollection sections)
		{
			var lastContent = sections[0].Content;
			for (var nodeIndex = 0; nodeIndex < lastContent.Count; nodeIndex++)
			{
				if (lastContent[nodeIndex] is SiteTemplateNode template &&
					this.footerTemplates.Contains(template.Title))
				{
					var footerSection = new Section(null, new WikiNodeCollection(lastContent.Factory));
					footerSection.Content.AddRange(lastContent[nodeIndex..]);
					sections.Add(footerSection);
					for (var deleteIndex = lastContent.Count - 1; deleteIndex >= nodeIndex; --deleteIndex)
					{
						lastContent.RemoveAt(deleteIndex);
					}

					break; // Redundant, but used for safety to avoid any possibility of further looping.
				}
			}
		}
		#endregion

		#region Private Static Methods
		private void GetRefUses()
		{
			var refUsesFile = new CsvFile(GameInfo.Starfield.ModFolder + "RefUses.csv")
			{
				Encoding = Encoding.GetEncoding(1252)
			};

			refUsesFile.Load();
			foreach (var row in refUsesFile)
			{
				var reference = row["Reference"];
				if (reference.Length == 10 && reference.StartsWith("0x", StringComparison.Ordinal))
				{
					reference = reference[2..];
				}

				var fullName = row["FullName"].Trim();
				var count = int.Parse(row["Uses"], CultureInfo.CurrentCulture);
				if (fullName.Length != 0 && count != 0)
				{
					if (!this.refUses.TryGetValue(reference, out var list))
					{
						list = [];
						this.refUses.Add(reference, list);
					}

					list.Add(new RefUse(fullName, count));
				}
			}
		}
		#endregion

		#region Private Records
		private record RefUse(string FullName, int Count);
		#endregion
	}
}