namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	[method: JobInfo("One-Off Parse Job")]
	public class OneOffParseJob(JobManager jobManager) : ParsedPageJob(jobManager)
	{
		#region Fields
		private readonly Dictionary<Title, BookInfo> bookLookup = [];
		private readonly UespNamespaceList namespaceList = new(jobManager.Site);
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Make interview refs consistent";

		protected override void LoadPages()
		{
			this.bookLookup.Add(TitleFactory.FromUnvalidated(this.Site, "General:Skeleton Man's Interview with Denizens of Tamriel"), new("People of Morrowind", "PoM"));
			this.bookLookup.Add(
				TitleFactory.FromUnvalidated(this.Site, "General:People of Morrowind"),
				new("People of Morrowind", "PoM"));
			this.bookLookup.Add(
				TitleFactory.FromUnvalidated(this.Site, "General:Interview with a Dark Elf"),
				new("Interview with a Dark Elf", "IWADE"));
			this.bookLookup.Add(
				TitleFactory.FromUnvalidated(this.Site, "Morrowind:Interview with a Dark Elf"),
				new("Interview with a Dark Elf", "IWADE"));
			this.bookLookup.Add(
				TitleFactory.FromUnvalidated(this.Site, "General:Interview With Three Booksellers"),
				new("Interview With Three Booksellers", "IWTB"));
			this.bookLookup.Add(
				TitleFactory.FromUnvalidated(this.Site, "General:Interview With Two Denizens of the Shivering Isles"),
				new("Interview With Two Denizens of the Shivering Isles", "IWTDSI"));
			this.bookLookup.Add(
				TitleFactory.FromUnvalidated(this.Site, "General:Homestead Interview"),
				new("Homestead Interview", "HomesteadInt"));
			this.bookLookup.Add(
				TitleFactory.FromUnvalidated(this.Site, "General:A Matter of Voice and Brass: Dragon Bones DLC Interview"),
				new("A Matter of Voice and Brass: Dragon Bones DLC Interview", "AMOVAB"));
			this.bookLookup.Add(
				TitleFactory.FromUnvalidated(this.Site, "General:New Life Festival Interview"),
				new("New Life Festival Interview", "NLFI"));

			this.Pages.GetBacklinks("Template:Ref", BacklinksTypes.EmbeddedIn);
		}

		protected override void ParseText(SiteParser parser)
		{
			var nameChanges = this.UpdateRefTargets(parser);
			UpdateRefNames(parser, nameChanges);
		}
		#endregion

		#region Private Static Methods
		private static void UpdateRefNames(SiteParser parser, Dictionary<string, string> nameChanges)
		{
			foreach (var reference in parser.FindSiteTemplates("Ref"))
			{
				if (reference.Find("name") is IParameterNode nameParam)
				{
					var nameValue = nameParam.Value;
					var name = nameValue.ToRaw().Trim();
					if (nameChanges.TryGetValue(name, out var newName))
					{
						nameValue.Clear();
						nameValue.AddText(newName);
						if (string.Equals(reference.GetValue("group"), "UOL", StringComparison.Ordinal))
						{
							reference.Remove("group");
						}

						reference.SetTitle("Ref");
						reference.Sort("name", "group");
					}
				}
			}
		}
		#endregion

		#region Private Methods
		private BookInfo? FindBookInfo(SiteParser parser, WikiNodeCollection target)
		{
			var targetText = target.ToRaw().Trim();
			var bookInfo = (string.Equals(targetText, "{{TIL|Interview with Three Argonians in Shadowfen|interview-three-argonians-shadowfen}}", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(targetText, "[https://www.imperial-library.info/content/interview-three-argonians-shadowfen Interview with Three Argonians in Shadowfen]", StringComparison.OrdinalIgnoreCase))
				? new("Interview with Three Argonians in Shadowfen", "IWTAS")
				: this.GetBookNameForRef(parser.Title, target);
			return bookInfo;
		}

		private BookInfo? GetBookNameForRef(Title title, WikiNodeCollection target)
		{
			var targetIndex = 0;
			if (target.Count == 3 &&
				target[0] is ITextNode open &&
				string.Equals(open.Text, "''", StringComparison.Ordinal) &&
				target[2] is ITextNode close &&
				string.Equals(close.Text, "''", StringComparison.Ordinal))
			{
				targetIndex = 1;
			}
			else if (target.Count != 1)
			{
				return null;
			}

			return target[targetIndex] switch
			{
				SiteTemplateNode template => template.Title.PageNameEquals("Cite Book")
					? this.GetCiteBookTarget(title, template)
					: null,
				SiteLinkNode link => this.bookLookup.TryGetValue(link.Title, out var retval)
					? retval
					: null,
				_ => null,
			};
		}

		private BookInfo? GetCiteBookTarget(Title title, SiteTemplateNode template)
		{
			var nsParamValue = template.Find("ns_base", "ns_id")?.Value.ToValue().Trim();
			var ns = this.namespaceList.GetNsBase(nsParamValue, title) ?? throw new InvalidOperationException();
			var pageName = template.GetValue(1);
			var templateTarget = TitleFactory.FromUnvalidated(this.Site, ns.Full + pageName);
			return this.bookLookup.TryGetValue(templateTarget, out var retval)
				? retval
				: null;
		}

		private Dictionary<string, string> UpdateRefTargets(SiteParser parser)
		{
			var retval = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (var reference in parser.FindSiteTemplates("Ref"))
			{
				var targetParam = reference.Find(1);
				if (targetParam is null ||
					targetParam.Value.Count == 0 ||
					this.FindBookInfo(parser, targetParam.Value) is not BookInfo bookInfo)
				{
					continue;
				}

				targetParam.Anonymize();
				targetParam.Value.Clear();
				targetParam.Value.AddParsed("{{Cite Book|" + bookInfo.FullName + "|ns_base=GEN}}");
				var nameParam = reference.Find("name");

				// Note: we're adding even same-named entries to retval so that the updater will remove group=UOL from ALL appropriate refs.
				if (nameParam is null)
				{
					reference.Add("name", bookInfo.ShortName);
					retval.TryAdd(bookInfo.ShortName, bookInfo.ShortName);
				}
				else
				{
					var name = nameParam.Value.ToRaw().Trim();
					retval.TryAdd(name, bookInfo.ShortName);
				}
			}

			return retval;
		}
		#endregion

		#region Private Records
		private record BookInfo(string FullName, string ShortName);
		#endregion
	}
}