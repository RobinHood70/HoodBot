namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	public class MovePages : MovePagesJob
	{
		#region Constructors
		[JobInfo("Move Pages")]
		public MovePages(JobManager jobManager)
				: base(jobManager)
		{
			this.MoveAction = MoveAction.None;
			this.EditSummaryMove = "Match page name to item";
		}
		#endregion

		#region Protected Override Methods

		protected override void UpdateLinkNode(Page page, ILinkNode node, bool isRedirectTarget)
		{
			page.ThrowNull();
			node.ThrowNull();
			var from = SiteLink.FromLinkNode(this.Site, node);
			if (this.LinkUpdateMatch(from) is Title to)
			{
				this.GetToLink(page, isRedirectTarget, from, to)
					.UpdateLinkNode(node);
			}

			if (from.Namespace == MediaWikiNamespaces.Media)
			{
				Title key = TitleFactory.FromValidated(this.Site[MediaWikiNamespaces.File], from.PageName);
				if (this.LinkUpdates.TryGetValue(key, out var toMedia))
				{
					this.GetToLink(page, isRedirectTarget, from, TitleFactory.FromValidated(this.Site[MediaWikiNamespaces.Media], toMedia!.PageName))
						.UpdateLinkNode(node);
				}
			}
		}

		protected override SiteLink GetToLink(Title page, bool isRedirectTarget, SiteLink from, Title to)
		{
			if (from.Interwiki == null && from.Fragment != null)
			{
				var newLink = from.WithTitle(TitleFactory.FromUnvalidated(to.Namespace, from.Fragment));
				newLink.Fragment = null;
				if (this.GetLinkText(page, from, newLink, !isRedirectTarget) is string newText)
				{
					newLink.Text = newText;
				}

				return newLink;
			}

			return base.GetToLink(page, isRedirectTarget, from, to);
		}

		protected override void PopulateMoves() => this.AddLinkUpdate("Online:Pets A#Ascadian Cliff Strider", "Online:Abecean Ratter Cat");

		/*
			//// this.AddReplacement("Skyrim:Map Notes", "Skyrim:Treasure Maps");
			//// this.LoadReplacementsFromFile(UespSite.GetBotDataFolder("Comma Replacements5.txt"));
			var fileName = Path.Combine(UespSite.GetBotDataFolder("Comma Replacements Oops.txt"));
			var repFile = File.ReadLines(fileName);
			var firstReps = new Dictionary<string, string>(StringComparer.Ordinal);
			foreach (var line in repFile)
			{
			var rep = line.Split(TextArrays.Tab);
			firstReps.Add(rep[0].Trim(), rep[1].Trim());
			}

			foreach (var rep in firstReps)
			{
			var value = rep.Value;
			while (firstReps.TryGetValue(value, out var rep2))
			{
			value = rep2;
			firstReps.Remove(rep.Key);

			}

			if (!string.Equals(rep.Key, value, StringComparison.Ordinal))
			{
			this.AddReplacement(rep.Key, value);
			}
			}
		*/
		#endregion
	}
}