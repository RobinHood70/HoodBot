namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	public class ModspaceProjectRealMoves : MovePagesJob
	{
		#region Constructors
		[JobInfo("1-Move mod categories", "Modspace")]
		public ModspaceProjectRealMoves(JobManager jobManager)
			: base(jobManager, "Categories")
		{
			this.DeleteStatusFile();
			this.EditSummaryMove = "Modspace Project: move pages";
			this.FollowUpActions = FollowUpActions.EmitReport | FollowUpActions.FixLinks | FollowUpActions.UpdateCategoryMembers | FollowUpActions.CheckLinksRemaining;
			this.RecursiveCategoryMembers = false; // It's unlikely anything new will be caught by recursing; it's a waste of time that'll easily be caught by Wanted Categories afterwardes.
			this.SuppressRedirects = true;
		}
		#endregion

		#region Protected Override Methods
		protected override void BacklinkPageLoaded(object sender, Page page)
		{
			base.BacklinkPageLoaded(sender.NotNull(nameof(sender)), page.NotNull(nameof(page)));
			page.Text = page?.Text.Replace("Tamriel_Data", "Tamriel Data", StringComparison.OrdinalIgnoreCase);
		}

		protected override void GetCategoryMembers()
		{
			if (this.Replacements.Count > 0)
			{
				var sb = new StringBuilder();
				foreach (var replacement in this.Replacements)
				{
					if (replacement.FromPage != null
						&& replacement.From.Namespace == MediaWikiNamespaces.Category
						&& replacement.To.Namespace == MediaWikiNamespaces.Category)
					{
						sb
							.Append(", '")
							.Append(replacement.From.PageName
								.Replace("'", "''", StringComparison.Ordinal)
								.Replace(" ", "_", StringComparison.Ordinal))
							.Append('\'');
					}
				}

				if (sb.Length > 0)
				{
					sb.Remove(0, 2);
					var db2 = App.GetConnectionString(this.Site.BaseArticlePath.Contains("dev.", StringComparison.Ordinal) ? "Dev" : "UespWiki");
					var query = $"SELECT DISTINCT page_namespace, page_title FROM categorylinks LEFT JOIN page ON categorylinks.cl_from = page.page_id WHERE cl_to IN({sb})";
					foreach (var row in Database.RunQuery(db2, query))
					{
						var ns = (int)row["page_namespace"];
						var name = (string)row["page_title"];
						this.BacklinkTitles.Add(TitleFactory.FromName(this.Site[ns], name));
					}
				}
			}
		}

		protected override void PopulateReplacements()
		{
			// Pseudospaces must come before their parents or they'll be added incorrectly.
			this.AddCategories();
			this.AddPseudospace(UespNamespaces.Tes3Mod, "Morrowind Rebirth", "Morrowind Rebirth", string.Empty);
			this.AddPseudospace(UespNamespaces.Tes3Mod, "Tamriel Data", "Tamriel Data", string.Empty);
			this.AddPseudospace(UespNamespaces.Tes3Mod, "Tamriel Rebuilt", "Tamriel Rebuilt", string.Empty);
			this.AddPseudospace(UespNamespaces.Tes3Mod, "Province: Cyrodiil", "Project Tamriel", "Cyrodiil/");
			this.AddPseudospace(UespNamespaces.Tes3Mod, "Skyrim: Home of the Nords", "Project Tamriel", "Skyrim/");

			this.AddPseudospace(UespNamespaces.Tes4Mod, "Better Cities", "Better Cities", string.Empty);

			this.AddReplacement("Tes5Mod:Beyond Skyrim: BSAssets", "Beyond Skyrim:BSAssets");
			this.AddReplacement("Tes5Mod:Beyond Skyrim: Wares of Tamriel", "Beyond Skyrim:Wares of Tamriel/Main Page");
			this.AddPseudospace(UespNamespaces.Tes5Mod, "Beyond Skyrim: BSAssets/", "Beyond Skyrim", string.Empty);
			this.AddPseudospace(UespNamespaces.Tes5Mod, "Beyond Skyrim: Cyrodiil", "Beyond Skyrim", "Cyrodiil/");
			this.AddPseudospace(UespNamespaces.Tes5Mod, "Beyond Skyrim/", "Beyond Skyrim", "Wares of Tamriel/");
			this.AddPseudospace(UespNamespaces.Tes5Mod, "Beyond Skyrim", "Beyond Skyrim", string.Empty);

			var verification = new HashSet<Title>(SimpleTitleComparer.Instance);
			var remove = new List<Title>();
			foreach (var replacement in this.Replacements)
			{
				if (!verification.Add(replacement.To))
				{
					remove.Add(replacement.From);
					Debug.WriteLine($"Duplicate To page: {replacement.From} -> {replacement.To}");
				}
			}

			foreach (var title in remove)
			{
				this.Replacements.Remove(title);
			}
		}

		protected override void UpdateTemplateNode(Page page, SiteTemplateNode template)
		{
			base.UpdateTemplateNode(page, template);
			foreach (var nsBase in template.FindAll("ns_base", "ns_id"))
			{
				var newBase = nsBase.Value.ToValue() switch
				{
					"BS5" => "BS5WOT",
					"BSA" => "BS5",
					"Tes3Mod:Morrowind Rebirth" => "Morrowind Rebirth",
					"Tes3Mod:Province: Cyrodiil" => "Project Tamriel:Cyrodiil",
					"Tes3Mod:Skyrim: Home of the Nords" => "Project Tamriel:Skyrim",
					"Tes3Mod:Tamriel Data" => "Tamriel Data",
					"Tes3Mod:Tamriel Rebuilt" => "Tamriel Rebuilt",
					"Tes4Mod:Better Cities" => "Better Cities",
					"Tes5Mod:Beyond Skyrim" => "Beyond Skyrim:Wares of Tamriel",
					"Tes5Mod:Beyond Skyrim: BSAssets " => "Beyond Skyrim",
					"Tes5Mod:Beyond Skyrim: Cyrodiil" => "Beyond Skyrim:Cyrodiil",
					"Tes2Mod:Daggerfall Unity" => "Daggerfall Mod:Daggerfall Unity",
					_ => null,
				};

				if (newBase != null)
				{
					nsBase.SetValue(newBase);
				}
			}
		}
		#endregion

		#region Private Methods
		private void AddCategories()
		{
			var uespNamespaceList = new UespNamespaceList(this.Site);
			var nsMap = new List<(string, UespNamespace)>()
			{
				("Tes3Mod-Skyrim: Home of the Nords", uespNamespaceList["Project Tamriel:Skyrim"]),
				("Tes5Mod-Beyond Skyrim-BSAssets", uespNamespaceList["Beyond Skyrim"]),
				("Tes5Mod-Beyond Skyrim-Cyrodiil", uespNamespaceList["Beyond Skyrim:Cyrodiil"]),
				("Tes3Mod-Province: Cyrodiil", uespNamespaceList["Project Tamriel:Cyrodiil"]),
				("Tes3Mod-Morrowind Rebirth", uespNamespaceList["Morrowind Rebirth"]),
				("Tes2Mod-Daggerfall Unity", uespNamespaceList["Daggerfall Mod:Daggerfall Unity"]),
				("Tes3Mod-Tamriel Rebuilt", uespNamespaceList["Tamriel Rebuilt"]),
				("Tes4Mod-Better Cities", uespNamespaceList["Better Cities"]),
				("Tes5Mod-Beyond Skyrim", uespNamespaceList["Beyond Skyrim"]),
				("Tes3Mod-Tamriel Data", uespNamespaceList["Tamriel Data"]),
				("Tes4Mod-Stirk", uespNamespaceList["Oblivion Mod:Stirk"]),
				("TesOtherMod", uespNamespaceList["Mod"]),
				("Tes1Mod", uespNamespaceList["Arena Mod"]),
				("Tes2Mod", uespNamespaceList["Daggerfall Mod"]),
				("Tes3Mod", uespNamespaceList["Morrowind Mod"]),
				("Tes4Mod", uespNamespaceList["Oblivion Mod"]),
				("Tes5Mod", uespNamespaceList["Skyrim Mod"]),
				("ESOMod", uespNamespaceList["Online Mod"]),
			};

			var allCats = new TitleCollection(this.Site);
			foreach (var (oldCategory, newNamespace) in nsMap)
			{
				if (!oldCategory.Contains('-', StringComparison.Ordinal) &&
					!string.Equals(oldCategory, newNamespace.Category, StringComparison.Ordinal))
				{
					allCats.GetCategories(oldCategory);
					allCats.GetNamespace(UespNamespaces.Category, Filter.Any, oldCategory);
				}
			}

			foreach (var cat in allCats)
			{
				Title newTitle = null;
				foreach (var (oldCategory, newNamespace) in nsMap)
				{
					if (cat.PageName.StartsWith(oldCategory, StringComparison.Ordinal))
					{
						newTitle = TitleFactory.FromName(this.Site[UespNamespaces.Category], newNamespace.Category + cat.PageName[oldCategory.Length..]);
						break;
					}
				}

				if (newTitle == null)
				{
					Debug.Assert(false, "No matching prefix found. This should never happen.");
				}
				else
				{
					this.AddReplacement(cat, newTitle);
				}
			}
		}

		private void AddPseudospace(int nsNum, string pseudoSpace, string newNsName, string newPseudoSpace)
		{
			var ns = this.Site[nsNum];
			var titles = new TitleCollection(this.Site);
			titles.GetNamespace(nsNum, Filter.Any, pseudoSpace);
			titles.GetNamespace(ns.TalkSpace!.Id, Filter.Any, pseudoSpace);
			foreach (var title in titles)
			{
				var slashIndex = title.PageName.IndexOf('/', StringComparison.Ordinal) + 1;
				var pageName = slashIndex == 0
					? title.PageName.Replace(newNsName + ": ", string.Empty, StringComparison.Ordinal)
					: title.PageName[slashIndex..];
				var newSpace = newNsName + (title.Namespace.IsTalkSpace ? " talk" : string.Empty) + ':';
				var newTitle = TitleFactory.FromName(this.Site, newSpace + newPseudoSpace + pageName);
				//// Debug.WriteLine($"{title}\t{newTitle}");
				if (!this.Replacements.Contains(title))
				{
					this.Replacements.Add(new Replacement(title, newTitle));
				}
			}
		}
		#endregion
	}
}