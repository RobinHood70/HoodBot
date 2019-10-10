namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiClasses;
	using RobinHood70.WikiClasses.Parser;
	using RobinHood70.WikiCommon;

	public class UntranscludeLore : EditJob
	{
		#region Static Fields
		private static readonly TemplateNode OldTransclusionNode = new TemplateNode(false, new NodeCollection(new TextNode("Old Lore Transclusion")), null);
		#endregion

		#region Fields
		private readonly TitleCollection allPageNames;
		private readonly PageCollection gamePages;
		private readonly PageCollection lorePages;
		private Page currentPage;
		private Page currentLorePage;
		private Dictionary<string, NodeCollection> transclusionParameters;
		private HashSet<Namespace> linkedNamespaces;
		private bool noTransclusions;
		private bool templateHappened;
		#endregion

		#region Constructors
		[JobInfo("Untransclude Lore")]
		public UntranscludeLore([ValidatedNotNull] Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
			this.allPageNames = new TitleCollection(site);
			this.gamePages = new PageCollection(site);
			this.lorePages = new PageCollection(site);
		}
		#endregion

		#region Public Override Properties
		public override string LogName => "Untransclude Lore";
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			this.ProgressMaximum = this.gamePages.Count + this.lorePages.Count;
			this.StatusWriteLine("Saving Game Pages");
			foreach (var page in this.gamePages)
			{
				page.Save("Untransclude Lore Pages", true);
				this.Progress++;
			}

			this.StatusWriteLine("Saving Lore Pages");
			foreach (var page in this.lorePages)
			{
				page.Save("Untransclude Lore Pages", true);
				this.Progress++;
			}
		}

		protected override void PrepareJob()
		{
			MetaNamespace.InitializeNamespaces(this.Site);
			this.StatusWriteLine("Loading Pages");
			this.GetPages();

			this.StatusWriteLine("Updating Game Pages");
			this.UpdateGamePages();

			this.StatusWriteLine("Updating Lore Pages");
			this.UpdateLorePages();
		}
		#endregion

		#region Private Static Methods
		private static WikiNode IfEqualsReplacer(TemplateNode node, string templateName)
		{
			var parameters = node.ParameterDictionary();
			var testValue = templateName.Split(TextArrays.Colon, 2)[1].Trim();
			var against = WikiTextVisitor.Value(parameters["1"]).Trim();
			return testValue == against
				? (parameters.TryGetValue("2", out var trueValue) && trueValue.Count > 0) ? trueValue : null
				: (parameters.TryGetValue("3", out var falseValue) && falseValue.Count > 0) ? falseValue : null;
		}
		#endregion

		#region Private Methods
		private WikiNode FmiReplacer(TemplateNode node)
		{
			if (this.currentPage.Namespace == UespNamespaces.Lore)
			{
				return (this.noTransclusions || node.ParameterDictionary().ContainsKey("nolore")) ? null : node;
			}
			else
			{
				node.Parameters.Clear();
				if (this.currentPage.PageName != this.currentLorePage.PageName)
				{
					node.Parameters.Add(new ParameterNode(1, new NodeCollection(new TextNode(this.currentLorePage.PageName))));
				}

				return node;
			}
		}

		private void GetPages()
		{
			var allPages = new PageCollection(this.Site, new PageLoadOptions(PageModules.Default | PageModules.TranscludedIn, true));
			allPages.GetPageLinks(new[] { new Title(this.Site, this.Site.User.FullPageName + "/Lore Transclusions") });
			allPages.Sort();

			var namespaces = new HashSet<Namespace>();
			foreach (var page in allPages)
			{
				page.Text = WikiTextUtilities.RemoveInivisibleCharacters(page.Text);
				if (!namespaces.Contains(page.Namespace))
				{
					namespaces.Add(page.Namespace);
					this.allPageNames.GetNamespace(page.Namespace.Id, Filter.Any);
				}

				if (page.Namespace == UespNamespaces.Lore)
				{
					this.lorePages.Add(page);
				}
				else
				{
					this.gamePages.Add(page);
				}
			}
		}

		private WikiNode LoreLinkReplacer(TemplateNode node)
		{
			var parameters = node.ParameterDictionary();
			var metaNamespace = MetaNamespace.FromTitle(this.currentPage);
			if (!parameters.TryGetValue(metaNamespace.Id + "link", out var linkParam))
			{
				linkParam = parameters["1"];
			}

			var link = WikiTextVisitor.Value(linkParam);
			var linkTitle = Title.DefaultToNamespace(this.currentPage.Namespace, link);
			if (!parameters.TryGetValue(metaNamespace + "display", out var displayParam))
			{
				parameters.TryGetValue("2", out displayParam);
			}

			var display = displayParam == null ? linkTitle.LabelName : WikiTextVisitor.Value(displayParam);

			if (
				!this.allPageNames.TryGetValue(linkTitle, out var linkPage) &&
				metaNamespace.Parent != null &&
				!this.allPageNames.TryGetValue(new Title(metaNamespace.Parent, linkTitle.PageName), out linkPage))
			{
				node.Title.Clear();
				node.Title.Add(new TextNode("Future Link"));
				linkParam.Parent = null;
				node.Parameters.Clear();
				node.Parameters.Add(new ParameterNode(1, linkParam));
				if (displayParam != null)
				{
					displayParam.Parent = null;
					node.Parameters.Add(new ParameterNode(2, displayParam));
				}

				// If no match is found in the current gamespace or its parent, do not modify the Future/Lore Link. Logic is the same regardless of whether we're in Lore or Gamespace.
				return node;
			}

			if (this.noTransclusions && linkTitle.SimpleEquals(this.currentPage))
			{
				return new TextNode($"'''{display}'''");
			}

			var titleNodes = new NodeCollection { new TextNode(linkPage.FullPageName) };
			var displayNode = new ParameterNode(1, new NodeCollection { new TextNode(display) });
			return new LinkNode(titleNodes, new[] { displayNode });
		}

		private WikiNode LoreTransclusionReplacer(WikiNode node)
		{
			if (node is TemplateNode templateNode)
			{
				var templateName = WikiTextVisitor.Value(templateNode.Title).Trim();
				var templateTitle = Title.DefaultToNamespace(this.Site, MediaWikiNamespaces.Template, templateName);
				if (templateTitle.Namespace == UespNamespaces.Lore && this.currentPage.Namespace != UespNamespaces.Lore)
				{
					if (!this.lorePages.TryGetValue(templateTitle, out var lorePage))
					{
						throw new InvalidOperationException();
					}

					this.currentLorePage = lorePage;
					var fixedUpLoreText = lorePage.Text;

					// Total kludge, but replacing across different nodes is currently hard, where this is not. The purpose is to avoid resultant text like '''Breton'''s.
					foreach (var name in new[] { "Ayleid", "Breton", "Naga", "Nede", "Orc", "Redguard" })
					{
						if (lorePage.PageName == name)
						{
							fixedUpLoreText = fixedUpLoreText.Replace("{{Lore Link|" + name + "}}s", "{{Lore Link|" + name + "|" + name + "s}}");
						}
					}

					var loreText = WikiTextParser.Parse(fixedUpLoreText, true, true);
					if (templateNode.AtLineStart && loreText[0] is TextNode textNode)
					{
						textNode.Text = textNode.Text.TrimStart();
						if (textNode.Text.Length == 0)
						{
							loreText.RemoveAt(0);
						}
					}

					this.transclusionParameters = templateNode.ParameterDictionary();
					loreText.Replace(this.TemplateReplacer);

					return loreText;
				}
			}

			return node;
		}

		private WikiNode NstReplacer(TemplateNode node)
		{
			var parameters = node.ParameterDictionary();
			var nsId = MetaNamespace.FromTitle(this.currentPage).Id;
			if (!parameters.TryGetValue(nsId, out var retval))
			{
				parameters.TryGetValue("1", out retval);
			}

			return retval;
		}

		private WikiNode OldLoreInserter(WikiNode node)
		{
			if (node is TemplateNode templateNode)
			{
				var templateName = WikiTextVisitor.Value(templateNode.Title).Trim();
				var templateTitle = Title.DefaultToNamespace(this.Site, MediaWikiNamespaces.Template, templateName);
				if (templateTitle.Namespace == UespNamespaces.Template && templateTitle.PageName.Contains("Trail"))
				{
					if (!this.templateHappened)
					{
						this.templateHappened = true;
						return new NodeCollection(templateNode, OldTransclusionNode);
					}
				}
			}

			return node;
		}

		private WikiNode TemplateReplacer(WikiNode node)
		{
			if (this.transclusionParameters != null && node is ArgumentNode arg)
			{
				var argName = WikiTextVisitor.Value(arg.Name);
				this.transclusionParameters.TryGetValue(argName, out var retval);

				return retval ?? arg.DefaultValue;
			}

			if (node is TemplateNode templateNode)
			{
				var templateName = WikiTextVisitor.Value(templateNode.Title).Trim();
				var templateTitle = Title.DefaultToNamespace(this.Site, MediaWikiNamespaces.Template, templateName);
				if (templateTitle.Namespace == UespNamespaces.Template)
				{
					switch (templateTitle.PageName)
					{
						case "Cite Book":
						case "Disambig":
						case "NewLeft":
						case "NewLine":
						case "Stub":
						case "TIL":
						case "Year":
							break;
						case "Cite book":
							templateNode.Title.Clear();
							templateNode.Title.Add(new TextNode("Cite Book"));
							return node;
						case "FMI":
							return this.FmiReplacer(templateNode);
						case "Lore Link":
							return this.LoreLinkReplacer(templateNode);
						case "Nst":
							return this.NstReplacer(templateNode);
						case "Tense":
							return this.TenseReplacer(templateNode);
						case "NAMESPACE":
							return new TextNode(this.currentPage.Namespace.Name);
						case "PAGENAME":
							return new TextNode(this.currentPage.PageName);
						case "Ref":
							return this.currentPage.Namespace == UespNamespaces.Lore ? node : null;
						default:
							if (templateNode.Title[0] is TextNode textNode && textNode.Text.Trim() == "#ifeq:")
							{
								return IfEqualsReplacer(templateNode, templateName);
							}

							if (this.currentPage.Namespace != UespNamespaces.Lore)
							{
								Debug.WriteLine($"{WikiTextVisitor.Raw(templateNode)} transcluding onto [[{this.currentPage.FullPageName}]]");
							}

							return node;
					}
				}
			}

			return node;
		}

		private WikiNode TenseReplacer(TemplateNode node)
		{
			var parameters = node.ParameterDictionary();
			var parentNamespace = MetaNamespace.ParentFromTitle(this.currentPage);
			var paramToGet = parameters.ContainsKey(parentNamespace.Base) || parameters.ContainsKey(parentNamespace.Id) ? "1" : "2";
			parameters.TryGetValue(paramToGet, out var retval);
			return retval;
		}

		private void UpdateGamePages()
		{
			foreach (var page in this.gamePages)
			{
				this.SetPageInfo(page);
				var parsedText = WikiTextParser.Parse(page.Text, this.noTransclusions ? false : null as bool?, true);
				this.templateHappened = false;
				parsedText.Replace(this.LoreTransclusionReplacer);
				parsedText.Replace(this.OldLoreInserter);
				this.ConfirmOldTransclusion(parsedText);
				this.transclusionParameters = null;
				page.Text = WikiTextVisitor.Raw(parsedText);
			}
		}

		private void UpdateLorePages()
		{
			foreach (var page in this.lorePages)
			{
				this.SetPageInfo(page);
				var parsedText = WikiTextParser.Parse(page.Text, this.noTransclusions ? false : null as bool?, true);
				this.templateHappened = false;
				parsedText.Replace(this.TemplateReplacer);
				parsedText.Replace(this.OldLoreInserter);
				this.ConfirmOldTransclusion(parsedText);
				page.Text = WikiTextVisitor.Raw(parsedText);

				// page.Text = page.Text.Replace("\xA0", "&nbsp;");
				// page.Text = Regex.Replace(page.Text, @"[ \t\r\v\u0085\u1680\u180E\u2000-\u200A\u2028\u2029\u202F\u205F\u3000]+\n", "\n")
			}
		}

		private void ConfirmOldTransclusion(NodeCollection parsedText)
		{
			if (!this.templateHappened)
			{
				var maxTags = parsedText.Count < 3 ? parsedText.Count : 3;
				for (var loc = 0; loc < maxTags; loc++)
				{
					if (parsedText[loc] is IgnoreNode tag && tag.Value == "<noinclude>")
					{
						parsedText.Insert(loc + 1, OldTransclusionNode);
						this.templateHappened = true;
						break;
					}
				}

				if (!this.templateHappened)
				{
					parsedText.Insert(0, OldTransclusionNode);
				}
			}

			OldTransclusionNode.Parent = null;
		}

		private void SetPageInfo(Page page)
		{
			this.currentPage = page; // Easier than passing through multiple levels of replacement.
			this.linkedNamespaces = new HashSet<Namespace>();
			foreach (var linkedPage in page.TranscludedIn)
			{
				if (!linkedPage.SimpleEquals(page))
				{
					if (this.currentPage.Namespace != UespNamespaces.Lore || !this.gamePages.Contains(linkedPage))
					{
						this.linkedNamespaces.Add(linkedPage.Namespace);
					}
				}
			}

			this.linkedNamespaces.Remove(this.Site.Namespaces[UespNamespaces.User]);
			this.noTransclusions = this.linkedNamespaces.Count == 0;
			if (this.noTransclusions)
			{
				Debug.WriteLine($"{page.FullPageName} has no tranclusions.");
			}
		}
		#endregion
	}
}