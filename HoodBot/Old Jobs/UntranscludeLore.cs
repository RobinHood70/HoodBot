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
		private static readonly TemplateNode OldTransclusionNode = TemplateNode.FromParts("Old Lore Transclusion");
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
		private static IWikiNode IfEqualsReplacer(TemplateNode node, string templateName)
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
		private void ConfirmOldTransclusion(NodeCollection parsedText)
		{
			if (!this.templateHappened)
			{
				var maxTags = parsedText.Count < 3 ? parsedText.Count : 3;
				var currentNode = parsedText.First;
				while (currentNode != null && maxTags > 0)
				{
					if (currentNode.Value is IgnoreNode tag && tag.Value == "<noinclude>")
					{
						parsedText.AddAfter(currentNode, OldTransclusionNode);
						this.templateHappened = true;
						break;
					}

					maxTags--;
				}

				if (!this.templateHappened)
				{
					parsedText.AddFirst(OldTransclusionNode);
				}
			}
		}

		private IWikiNode FmiReplacer(TemplateNode node)
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
					node.Parameters.Add(ParameterNode.FromParts(1, this.currentLorePage.PageName));
				}

				return node;
			}
		}

		private void GetPages()
		{
			var allPages = new PageCollection(this.Site, new PageLoadOptions(PageModules.Default | PageModules.TranscludedIn, true));
			//// allPages.GetPageLinks(new[] { new Title(this.Site, this.Site.User.FullPageName + "/Lore Transclusions") });
			allPages.GetTitles("General:The Elder Scrolls", "Lore:Elder Scrolls");
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

		private IWikiNode LoreLinkReplacer(TemplateNode node)
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
				node.Title.AddFirst(new TextNode("Future Link"));
				node.Parameters.Clear();
				node.Parameters.Add(new ParameterNode(1, linkParam));
				if (displayParam != null)
				{
					node.Parameters.Add(new ParameterNode(2, displayParam));
				}

				// If no match is found in the current gamespace or its parent, do not modify the Future/Lore Link. Logic is the same regardless of whether we're in Lore or Gamespace.
				return node;
			}

			return this.noTransclusions && linkTitle.SimpleEquals(this.currentPage)
				? new TextNode($"'''{display}'''")
				: LinkNode.FromParts(linkPage.FullPageName, display) as IWikiNode;
		}

		private IWikiNode LoreTransclusionReplacer(LinkedListNode<IWikiNode> node)
		{
			if (node.Value is TemplateNode templateNode)
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
					if (/* templateNode.AtLineStart && */loreText.First.Value is TextNode textNode)
					{
						textNode.Text = textNode.Text.TrimStart();
						if (textNode.Text.Length == 0)
						{
							loreText.RemoveFirst();
						}
					}

					this.transclusionParameters = templateNode.ParameterDictionary();
					loreText.Replace(this.TemplateReplacer);

					return loreText;
				}
			}

			return node.Value;
		}

		private IWikiNode NstReplacer(TemplateNode node)
		{
			var parameters = node.ParameterDictionary();
			var nsId = MetaNamespace.FromTitle(this.currentPage).Id;
			if (!parameters.TryGetValue(nsId, out var retval))
			{
				parameters.TryGetValue("1", out retval);
			}

			return retval;
		}

		private IWikiNode OldLoreInserter(LinkedListNode<IWikiNode> node)
		{
			if (node.Value is TemplateNode templateNode)
			{
				var templateName = WikiTextVisitor.Value(templateNode.Title).Trim();
				var templateTitle = Title.DefaultToNamespace(this.Site, MediaWikiNamespaces.Template, templateName);
				if (templateTitle.Namespace == UespNamespaces.Template && templateTitle.PageName.Contains("Trail"))
				{
					if (!this.templateHappened)
					{
						this.templateHappened = true;
						return new NodeCollection(null, new[] { templateNode, OldTransclusionNode });
					}
				}
			}

			return node.Value;
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
		}

		private IWikiNode TemplateReplacer(LinkedListNode<IWikiNode> node)
		{
			switch (node.Value)
			{
				case ArgumentNode arg:
					if (this.transclusionParameters != null)
					{
						var argName = WikiTextVisitor.Value(arg.Name);
						this.transclusionParameters.TryGetValue(argName, out var retval);

						return retval ?? arg.DefaultValue;
					}

					break;
				case TemplateNode templateNode:
					var templateName = WikiTextVisitor.Value(templateNode.Title).Trim();
					var templateTitle = Title.DefaultToNamespace(this.Site, MediaWikiNamespaces.Template, templateName);
					if (templateTitle.Namespace == UespNamespaces.Template)
					{
						if (templateTitle.PageName == "Cite book")
						{
							templateNode.Title.Clear();
							templateNode.Title.AddFirst(new TextNode("Cite Book"));
							break;
						}

						if (templateTitle.PageName == "Ref" && this.currentPage.Namespace != UespNamespaces.Lore)
						{
							// Has to be outside of switch because we use null for something else there and then change it to node.Value later.
							return null;
						}

						var retval = templateTitle.PageName switch
						{
							"Cite Book" => node.Value,
							"Disambig" => node.Value,
							"FMI" => this.FmiReplacer(templateNode),
							"Lore Link" => this.LoreLinkReplacer(templateNode),
							"NAMESPACE" => new TextNode(this.currentPage.Namespace.Name),
							"NewLeft" => node.Value,
							"NewLine" => node.Value,
							"Nst" => this.NstReplacer(templateNode),
							"PAGENAME" => new TextNode(this.currentPage.PageName),
							"Ref" => node.Value,
							"Stub" => node.Value,
							"Tense" => this.TenseReplacer(templateNode),
							"TIL" => node.Value,
							"Year" => node.Value,
							_ => (templateNode.Title.First.Value is TextNode textNode && textNode.Text.Trim() == "#ifeq:")
								? IfEqualsReplacer(templateNode, templateName)
								: null
						};

						if (retval == null && this.currentPage.Namespace != UespNamespaces.Lore)
						{
							Debug.WriteLine($"{WikiTextVisitor.Raw(templateNode)} transcluding onto [[{this.currentPage.FullPageName}]]");
						}

						return retval ?? node.Value;
					}

					break;
			}

			return node.Value;
		}

		private IWikiNode TenseReplacer(TemplateNode node)
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
		#endregion
	}
}