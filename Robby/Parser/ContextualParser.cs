﻿namespace RobinHood70.Robby.Parser
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;
	using static RobinHood70.CommonCode.Globals;

	public class ContextualParser
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="ContextualParser"/> class.</summary>
		/// <param name="page">The page to parse.</param>
		public ContextualParser(Page page)
			: this(page ?? throw ArgumentNull(nameof(page)), page.Text, InclusionType.Raw, false)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="ContextualParser"/> class.</summary>
		/// <param name="title">The <see cref="ISimpleTitle">title</see> the text will be on.</param>
		/// <param name="text">The text to parse.</param>
		public ContextualParser(ISimpleTitle title, string text)
			: this(title ?? throw ArgumentNull(nameof(title)), text, InclusionType.Raw, false)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="ContextualParser"/> class.</summary>
		/// <param name="page">The page to parse.</param>
		/// <param name="inclusionType">The inclusion type for the text. <see langword="true"/> to return text as if transcluded to another page; <see langword="false"/> to return local text only; <see langword="null"/> to return all text. In each case, any ignored text will be wrapped in an IgnoreNode.</param>
		/// <param name="strictInclusion"><see langword="true"/> if the output should exclude IgnoreNodes; otherwise <see langword="false"/>.</param>
		public ContextualParser(Page page, InclusionType inclusionType, bool strictInclusion)
			: this(page ?? throw ArgumentNull(nameof(page)), page.Text, inclusionType, strictInclusion)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="ContextualParser"/> class.</summary>
		/// <param name="title">The <see cref="ISimpleTitle">title</see> the text will be on.</param>
		/// <param name="text">The text to parse.</param>
		/// <param name="inclusionType">The inclusion type for the text. <see langword="true"/> to return text as if transcluded to another page; <see langword="false"/> to return local text only; <see langword="null"/> to return all text. In each case, any ignored text will be wrapped in an IgnoreNode.</param>
		/// <param name="strictInclusion"><see langword="true"/> if the output should exclude IgnoreNodes; otherwise <see langword="false"/>.</param>
		public ContextualParser(ISimpleTitle title, string text, InclusionType inclusionType, bool strictInclusion)
		{
			this.Title = title ?? throw ArgumentNull(nameof(title));
			this.Site = title.Namespace.Site;
			this.Nodes = NodeCollection.Parse(text ?? string.Empty, inclusionType, strictInclusion);
		}
		#endregion

		#region Public Properties
		public IEnumerable<HeaderNode> HeaderNodes => this.Nodes.FindAll<HeaderNode>();

		public IEnumerable<LinkNode> LinkNodes => this.Nodes.FindAll<LinkNode>();

		public IDictionary<string, Func<string>> MagicWordResolvers { get; } = new Dictionary<string, Func<string>>(StringComparer.Ordinal);

		public NodeCollection Nodes { get; }

		public IDictionary<string, string> Parameters { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

		public Site Site { get; }

		public IEnumerable<TemplateNode> TemplateNodes => this.Nodes.FindAll<TemplateNode>();

		public IEnumerable<TextNode> TextNodes => this.Nodes.FindAll<TextNode>();

		public IDictionary<string, Func<string>> TemplateResolvers { get; } = new Dictionary<string, Func<string>>(StringComparer.Ordinal);

		public ISimpleTitle Title { get; set; }
		#endregion

		#region Public Methods
		public bool AddCategory(string category)
		{
			ThrowNull(category, nameof(category));
			var catTitle = FullTitle.Coerce(this.Site, MediaWikiNamespaces.Category, category);
			LinkedListNode<IWikiNode>? lastCategory = null;
			foreach (var link in this.Nodes.FindAllListNodes<LinkNode>())
			{
				var linkNode = (LinkNode)link.Value;
				var title = FullTitle.FromBacklinkNode(this.Site, linkNode);
				if (title.Namespace == MediaWikiNamespaces.Category)
				{
					if (string.Equals(title.PageName, catTitle.PageName, StringComparison.Ordinal))
					{
						return false;
					}

					lastCategory = link;
				}
			}

			var newCat = LinkNode.FromParts(catTitle.ToString());
			if (lastCategory == null)
			{
				this.Nodes.AddLast(new TextNode("\n\n"));
				this.Nodes.AddLast(newCat);
			}
			else
			{
				this.Nodes.AddAfter(lastCategory, newCat);
			}

			return true;
		}

		/// <summary>Finds the first header with the specified text.</summary>
		/// <param name="headerText">Name of the header.</param>
		/// <returns>The first header with the specified text.</returns>
		/// <remarks>This is a temporary function until HeaderNode can be rewritten to work more like other nodes (i.e., without capturing trailing whitespace).</remarks>
		public LinkedListNode<IWikiNode>? FindFirstHeaderLinked(string headerText) => this.Nodes.FindListNode<HeaderNode>(header => string.Equals(header.GetInnerText(true), headerText, StringComparison.Ordinal), false, true, null);

		public LinkNode? FindLink(string find) => this.FindLink(new TitleParser(this.Site, find));

		public LinkNode? FindLink(ISimpleTitle find)
		{
			foreach (var link in this.FindLinks(find))
			{
				return link;
			}

			return null;
		}

		public LinkNode? FindLink(IFullTitle find)
		{
			foreach (var link in this.FindLinks(find))
			{
				return link;
			}

			return null;
		}

		public IEnumerable<LinkNode> FindLinks(string find) => this.FindLinks(new TitleParser(this.Site, find));

		public IEnumerable<LinkNode> FindLinks(ISimpleTitle find)
		{
			foreach (var link in this.LinkNodes)
			{
				var linkTitle = Robby.Title.FromBacklinkNode(this.Site, link);
				if (linkTitle.SimpleEquals(find))
				{
					yield return link;
				}
			}
		}

		public IEnumerable<LinkNode> FindLinks(IFullTitle find)
		{
			foreach (var link in this.LinkNodes)
			{
				var linkTitle = FullTitle.FromBacklinkNode(this.Site, link);
				if (linkTitle.FullEquals(find))
				{
					yield return link;
				}
			}
		}

		public TemplateNode? FindTemplate(string templateName)
		{
			foreach (var link in this.FindTemplates(templateName))
			{
				return link;
			}

			return null;
		}

		public TemplateNode? FindTemplateNode(string templateName)
		{
			foreach (var link in this.FindTemplates(templateName))
			{
				return link;
			}

			return null;
		}

		public IEnumerable<TemplateNode> FindTemplates(string templateName)
		{
			var find = new TitleParser(this.Site, MediaWikiNamespaces.Template, templateName);
			foreach (var template in this.TemplateNodes)
			{
				var titleText = template.GetTitleValue();
				var templateTitle = new TitleParser(this.Site, MediaWikiNamespaces.Template, titleText);
				if (templateTitle.SimpleEquals(find))
				{
					yield return template;
				}
			}
		}

		public IEnumerable<LinkedListNode<IWikiNode>> FindTemplateNodes(string templateName)
		{
			var find = new TitleParser(this.Site, MediaWikiNamespaces.Template, templateName);
			foreach (var templateNode in this.Nodes.FindAllListNodes<TemplateNode>())
			{
				if (templateNode.Value is TemplateNode template)
				{
					var titleText = template.GetTitleValue();
					var templateTitle = new TitleParser(this.Site, MediaWikiNamespaces.Template, titleText);
					if (templateTitle.SimpleEquals(find))
					{
						yield return templateNode;
					}
				}
			}
		}

		public string? GetText() => WikiTextVisitor.Raw(this.Nodes);
		#endregion
	}
}