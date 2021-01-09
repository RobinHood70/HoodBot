namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	public class ModspaceProjectUpdateLinks : MovePagesJob
	{
		#region Static Fields
		private static readonly int[] InPlaceNamespaces = new[]
		{
			UespNamespaces.Tes1Mod,
			UespNamespaces.Tes2Mod,
			UespNamespaces.Tes3Mod,
			UespNamespaces.Tes4Mod,
			UespNamespaces.Tes5Mod,
			UespNamespaces.EsoMod,
			UespNamespaces.TesOtherMod,
			UespNamespaces.Tes1ModTalk,
			UespNamespaces.Tes2ModTalk,
			UespNamespaces.Tes3ModTalk,
			UespNamespaces.Tes4ModTalk,
			UespNamespaces.Tes5ModTalk,
			UespNamespaces.EsoModTalk,
			UespNamespaces.TesOtherModTalk,
		};

		private static readonly (int Namespace, string PageName)[] SkipForSafety = new[]
		{
			(UespNamespaces.Tes2Mod, "Daggerfall Unity"),
			(UespNamespaces.Tes3Mod, "Morrowind Rebirth"),
			(UespNamespaces.Tes3Mod, "Province: Cyrodiil"),
			(UespNamespaces.Tes3Mod, "Skyrim: Home of the Nords"),
			(UespNamespaces.Tes3Mod, "Tamriel Data"),
			(UespNamespaces.Tes3Mod, "Tamriel Rebuilt"),
			(UespNamespaces.Tes4Mod, "Better Cities"),
			(UespNamespaces.Tes4Mod, "Stirk"),
			(UespNamespaces.Tes5Mod, "Beyond Skyrim"),
		};
		#endregion

		#region Constructors
		[JobInfo("3-In-place Link Updates", "Modspace Project")]
		public ModspaceProjectUpdateLinks(JobManager jobManager)
			: base(jobManager, "Links")
		{
			this.DeleteStatusFile();
			this.FollowUpActions = FollowUpActions.UpdateCaption | FollowUpActions.FixLinks;
			this.MoveAction = MoveAction.None;
		}
		#endregion

		#region Protected Override Methods
		protected override void GetBacklinkTitles()
		{
			base.GetBacklinkTitles();
			this.BacklinkTitles.Remove("UESPWiki:Community Portal/Archive 55");
		}

		protected override void PopulateReplacements()
		{
			var titles = new TitleCollection(this.Site);
			foreach (var ns in InPlaceNamespaces)
			{
				titles.GetNamespace(ns, Filter.Any);
			}

			foreach (var title in titles)
			{
				var updateThis = true;
				foreach (var skipTitle in SkipForSafety)
				{
					if (title.Namespace.SubjectSpace.Id == skipTitle.Namespace && title.PageName.StartsWith(skipTitle.PageName, StringComparison.Ordinal))
					{
						updateThis = false;
						break;
					}
				}

				if (updateThis)
				{
					this.AddReplacement(title, title); // Does nothing, goes nowhere. ;)
				}
			}
		}

		protected override void UpdateLinkText(Page page, Title oldTitle, SiteLink newLink, bool addCaption)
		{
			// Checking for "Mod" in text so this only changes a full namespace name and not a short one.
			base.UpdateLinkText(page, oldTitle, newLink, addCaption);
			if (newLink.Text != null
				&& newLink.Text.EndsWith("Mod", StringComparison.Ordinal)
				&& !this.Site.IsDiscussionPage(page)
				&& this.Site.Namespaces.TryGetValue(newLink.Text, out var ns)
				&& InPlaceNamespaces.Contains(ns.Id))
			{
				newLink.Text = ns.CanonicalName;
			}
		}

		protected override void UpdateTemplateNode(Page page, SiteTemplateNode template)
		{
			base.UpdateTemplateNode(page, template);
			foreach (var nsBase in template.FindAll("ns_base", "ns_id"))
			{
				var newBase = nsBase.Value.ToValue() switch
				{
					"ESOMod" => "ESO Mod",
					"T1" => "ARMOD",
					"T2" => "DFMOD",
					"T3" => "MWMOD",
					"T4" => "OBMOD",
					"T5" => "SRMOD",
					"TOTHER" => "MOD",
					"Tes1Mod" => "Arena Mod",
					"Tes2Mod" => "Daggerfall Mod",
					"Tes3Mod" => "Morrowind Mod",
					"Tes4Mod" => "Oblivion Mod",
					"Tes5Mod" => "Skyrim Mod",
					"TesOtherMod" => "Mod",
					_ => null,
				};

				if (newBase != null)
				{
					nsBase.SetValue(newBase);
				}
			}
		}
		#endregion
	}
}