namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	internal sealed class FutureLinkConverter : ParsedPageJob
	{
		#region Fields
		private readonly TitleCollection titles;
		private UespNamespaceList? namespaceList;
		#endregion

		#region Constructors
		[JobInfo("Future Link Converter", "Maintenance")]
		public FutureLinkConverter(JobManager jobManager)
			: base(jobManager)
		{
			this.titles = new TitleCollection(this.Site);
		}
		#endregion

		#region Protected Override Properties

		protected override string EditSummary => "Convert Future Link to hard link";
		#endregion

		#region Private Properties
		private UespNamespaceList NamespaceList => this.namespaceList ??= new UespNamespaceList(this.Site);

		#endregion

		#region Protected Override Methods
		protected override void BeforeLoadPages()
		{
			var templateCalls = new TitleCollection(this.Site);
			templateCalls.GetBacklinks("Template:Future Link", BacklinksTypes.EmbeddedIn, true);
			var namespaces = new HashSet<Namespace>();
			//// namespaces.Add(UespNamespaces.Lore);
			foreach (var title in templateCalls)
			{
				var ns = this.NamespaceList.FromTitle(title);
				if (ns.IsGameSpace)
				{
					namespaces.Add(ns.BaseNamespace);
					if (ns.Parent is Namespace nsParent)
					{
						namespaces.Add(nsParent);
					}
				}
			}

			this.ProgressMaximum = namespaces.Count;
			foreach (var ns in namespaces)
			{
				this.StatusWriteLine($"Getting {ns.Name} namespace");
				this.titles.GetNamespace(ns.Id);
				this.Progress++;
			}
		}

		protected override void LoadPages()
		{
			this.Pages.LoadOptions.Modules = PageModules.Default | PageModules.Backlinks;
			this.Pages.SetLimitations(LimitationType.Remove, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14);
			this.Pages.GetBacklinks("Template:Future Link", BacklinksTypes.EmbeddedIn, true);
		}

		protected override void ParseText(object sender, ContextualParser parser) =>
			parser.Replace(node => this.ReplaceNode(node, parser), false);

		private NodeCollection? ReplaceNode(IWikiNode node, ContextualParser parser)
		{
			NodeCollection? retval = null;
			if (node is SiteTemplateNode template &&
				template.TitleValue.PageNameEquals("Future Link"))
			{
				var nsBaseValue = template.Find("ns_base", "ns_id")?.Value.ToValue().Trim();
				var nsBase = this.NamespaceList.GetNsBase(parser.Page, nsBaseValue);
				if (nsBase is not null && template.GetValue(1) is string nameToFind)
				{
					var basePage = TitleFactory.FromUnvalidated(nsBase.BaseNamespace, nameToFind);
					Title? found;
					if (parser.Page.Namespace == UespNamespaces.Lore)
					{
						found = basePage;
					}
					else
					{
						/* Only applies to Lore Link, not Future Link
						var transcluded = false;
						foreach (var backlink in parser.Page.Backlinks)
						{
							if (backlink.Value == BacklinksTypes.EmbeddedIn && backlink.Key.Namespace != parser.Page.Namespace)
							{
								transcluded = true;
								break;
							}
						}

						if (!transcluded)
						{
							found = basePage;
						}
						else */
						{
							var namesToFind = new List<Title>
							{
								basePage,
								TitleFactory.FromUnvalidated(nsBase.Parent, nameToFind)
							};

							foreach (var name in namesToFind)
							{
								if (this.titles.TryGetValue(name, out found))
								{
									break;
								}
							}

							found = null;
						}
					}

					if (found != null)
					{
						var labelName = Title.ToLabelName(nameToFind);
						var displayName = template.GetValue(2) ?? labelName;
						retval = parser.Parse($"[[{found.FullPageName}|{displayName}]]");
					}
				}
			}

			return retval;
		}
		#endregion
	}
}
