namespace RobinHood70.Robby.Parser
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using RobinHood70.WikiCommon.Parser;

	/// <summary>Visitor to build wiki text, optionally ignoring text that has no value to the parser, such as comments or nowiki text.</summary>
	/// <seealso cref="IWikiNodeVisitor" />
	public class WikiPlainTextVisitor : IWikiNodeVisitor
	{
		#region Fields
		private readonly StringBuilder builder = new();
		private readonly Site site;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="WikiPlainTextVisitor"/> class.</summary>
		/// <param name="site">The site to use when resolving link nodes.</param>
		public WikiPlainTextVisitor(Site site)
		{
			ArgumentNullException.ThrowIfNull(site);
			this.site = site;
		}
		#endregion

		#region Public Methods

		/// <summary>Builds the specified node or node collection into wiki text.</summary>
		/// <param name="node">The node.</param>
		/// <returns>The wiki text of the collection.</returns>
		public string Build(IWikiNode node)
		{
			this.builder.Clear();
			ArgumentNullException.ThrowIfNull(node);
			node.Accept(this);
			return this.builder.ToString();
		}

		/// <summary>Builds the specified node or node collection into wiki text.</summary>
		/// <param name="nodes">The nodes.</param>
		/// <returns>The wiki text of the collection.</returns>
		public string Build(IEnumerable<IWikiNode>? nodes)
		{
			this.builder.Clear();
			if (nodes != null)
			{
				foreach (var node in nodes)
				{
					node.Accept(this);
				}
			}

			return this.builder.ToString();
		}
		#endregion

		#region IVisitor Methods

		/// <inheritdoc/>
		public void Visit(IArgumentNode node) => node?.DefaultValue?.Accept(this);

		/// <inheritdoc/>
		public void Visit(ICommentNode node)
		{
		}

		/// <inheritdoc/>
		public void Visit(IHeaderNode node)
		{
			ArgumentNullException.ThrowIfNull(node);
			node.Title.Accept(this);
		}

		/// <inheritdoc/>
		public void Visit(IIgnoreNode node)
		{
		}

		/// <inheritdoc/>
		public void Visit(ILinkNode node)
		{
			var link = SiteLink.FromLinkNode(this.site, node);
			this.builder.Append(link.Text);
		}

		/// <inheritdoc/>
		public void Visit(NodeCollection nodes)
		{
			ArgumentNullException.ThrowIfNull(nodes);
			foreach (var node in nodes)
			{
				node.Accept(this);
			}
		}

		/// <inheritdoc/>
		public void Visit(IParameterNode node)
		{
			ArgumentNullException.ThrowIfNull(node);

			// It would rarely make sense to resolve a parameter node in this context, but code is left unadulterated for special purposes like manually iterating through parameters in a link.
			this.builder.Append('|');
			if (node.Name is not null)
			{
				node.Name.Accept(this);
				this.builder.Append('=');
			}

			node.Value.Accept(this);
		}

		/// <inheritdoc/>
		public void Visit(ITagNode node)
		{
		}

		/// <inheritdoc/>
		public void Visit(ITemplateNode node)
		{
		}

		/// <inheritdoc/>
		public void Visit(ITextNode node)
		{
			ArgumentNullException.ThrowIfNull(node);
			this.builder.Append(node.Text);
		}
		#endregion
	}
}