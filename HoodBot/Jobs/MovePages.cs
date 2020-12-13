namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	public class MovePages : MovePagesJob
	{
		#region Constructors
		[JobInfo("Page Mover")]
		public MovePages(JobManager jobManager)
			: base(jobManager)
		{
			this.DeleteFiles();
			this.CustomReplaceGeneral = this.ReplaceInTemplates;
			this.EditSummaryMove = "Harmonize crafting motif page names";
		}
		#endregion

		#region Protected Override Methods
		protected override void PopulateReplacements() => this.LoadReplacementsFromFile(UespSite.GetBotDataFolder("Crafting Motif Name Chages.txt"));
		#endregion

		#region Private Methods
		private void ReplaceInTemplates(ContextualParser parser)
		{
			if (parser.FindTemplate("Game Book") is ITemplateNode gameBook &&
				gameBook.Find("lorename") is IParameterNode loreName &&
				loreName.Value.ToValue() is string loreNameValue &&
				new Title(this.Site[UespNamespaces.Lore], loreNameValue) is var title &&
				this.Replacements.TryGetValue(title, out var replacement) &&
				replacement.To is ISimpleTitle toPage)
			{
				loreName.SetValue(toPage.PageName + '\n');
			}

			foreach (var bookLink in parser.FindTemplates("Book Link"))
			{
				if (bookLink.Find(1) is IParameterNode link &&
					new Title(this.Site[UespNamespaces.Lore], link.Value.ToValue()) is var bookLinkTitle &&
					this.Replacements.TryGetValue(bookLinkTitle, out replacement) &&
					replacement.To is ISimpleTitle toLink)
				{
					link.Value.Clear();
					link.SetValue(toLink.PageName);
				}
			}

			foreach (var citeBook in parser.FindTemplates("Cite Book"))
			{
				if (citeBook.Find(1) is IParameterNode link &&
					new Title(this.Site[UespNamespaces.Lore], link.Value.ToValue()) is var citeBookTitle &&
					this.Replacements.TryGetValue(citeBookTitle, out replacement) &&
					replacement.To is ISimpleTitle toLink)
				{
					link.Value.Clear();
					link.SetValue(toLink.PageName);
				}
			}
		}
		#endregion
	}
}