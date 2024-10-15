namespace RobinHood70.WikiCommon.Parser
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	/// <summary>Visitor to build wiki text, optionally ignoring text that has no value to the parser, such as comments or nowiki text.</summary>
	/// <seealso cref="IWikiNodeVisitor" />
	/// <remarks>Initializes a new instance of the <see cref="WikiTextVisitor"/> class.</remarks>
	/// <param name="raw"><list type="bullet">
	/// <item>If set to <see langword="true"/>, returns only the value of each node, ignoring comments and the like. This is most useful for finding named items.</item>
	/// <item>If set to false, returns all nodes in the collection. This is most useful for editing the collection.</item>
	/// </list></param>
	public class WikiTextVisitor(bool raw) : IWikiNodeVisitor
	{
		#region Static Fields
		private static readonly WikiTextVisitor RawVisitor = new(true);
		private static readonly WikiTextVisitor ValueVisitor = new(false);
		#endregion

		#region Fields
		private readonly StringBuilder builder = new();
		private readonly bool raw = raw;
		#endregion

		#region Public Static Methods

		/// <summary>Returns the raw text for a node.</summary>
		/// <param name="node">The node.</param>
		/// <returns>The raw text for the specified node.</returns>
		public static string Raw(IWikiNode node) => RawVisitor.Build(node);

		/// <summary>Returns the raw text for a set of nodes.</summary>
		/// <param name="nodes">The nodes.</param>
		/// <returns>The raw text for the specified nodes.</returns>
		public static string Raw(IEnumerable<IWikiNode>? nodes) => RawVisitor.Build(nodes);

		/// <summary>Returns the value text for a node.</summary>
		/// <param name="node">The node.</param>
		/// <returns>The value text for the specified node.</returns>
		public static string Value(IWikiNode node) => ValueVisitor.Build(node);

		/// <summary>Returns the value text for a node collection.</summary>
		/// <param name="nodes">The nodes.</param>
		/// <returns>The value text for the specified nodes.</returns>
		public static string Value(IEnumerable<IWikiNode>? nodes) => ValueVisitor.Build(nodes);
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
		public void Visit(IArgumentNode node)
		{
			ArgumentNullException.ThrowIfNull(node);
			if (this.raw || node.DefaultValue == null)
			{
				this.builder.Append("{{{");
				node.Name.Accept(this);
				if (node.DefaultValue != null)
				{
					this.builder.Append('|');
					node.DefaultValue.Accept(this);
				}

				if (this.raw && node.ExtraValues != null)
				{
					foreach (var value in node.ExtraValues)
					{
						value.Accept(this);
					}
				}

				this.builder.Append("}}}");
			}
			else
			{
				node.DefaultValue.Accept(this);
			}
		}

		/// <inheritdoc/>
		public void Visit(ICommentNode node)
		{
			ArgumentNullException.ThrowIfNull(node);
			if (this.raw)
			{
				this.builder.Append(node.Comment);
			}
		}

		/// <inheritdoc/>
		public void Visit(IHeaderNode node)
		{
			ArgumentNullException.ThrowIfNull(node);
			var equalsSigns = new string('=', node.Level);
			this.builder.Append(equalsSigns);
			node.Title.Accept(this);
			this.builder.Append(equalsSigns);
			node.Comment?.Accept(this);
		}

		/// <inheritdoc/>
		public void Visit(IIgnoreNode node)
		{
			ArgumentNullException.ThrowIfNull(node);
			if (this.raw)
			{
				this.builder.Append(node.Value);
			}
		}

		/// <inheritdoc/>
		public void Visit(ILinkNode node)
		{
			ArgumentNullException.ThrowIfNull(node);
			if (this.raw)
			{
				this.builder.Append("[[");
				node.TitleNodes.Accept(this);
				foreach (var param in node.Parameters)
				{
					param.Accept(this);
				}

				this.builder.Append("]]");
			}
			else
			{
				if (node.Parameters.Count > 0)
				{
					foreach (var parameter in node.Parameters)
					{
						if (parameter.Name is not null)
						{
							parameter.Name.Accept(this);
							this.builder.Append('=');
						}

						parameter.Value.Accept(this);
					}
				}
				else
				{
					node.TitleNodes.Accept(this);
				}
			}
		}

		/// <inheritdoc/>
		public void Visit(WikiNodeCollection nodes)
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
			ArgumentNullException.ThrowIfNull(node);
			if (this.raw)
			{
				this.builder
					.Append('<')
					.Append(node.Name)
					.Append(node.Attributes);
				if (node.SelfClosed)
				{
					this.builder.Append("/>");
				}
				else
				{
					// TODO: Check what happens with <br> vs. <br/> vs. <br></br>. Do they all work properly?
					this.builder
						.Append('>')
						.Append(node.InnerText)
						.Append(node.Close);
				}
			}
		}

		/// <inheritdoc/>
		public void Visit(ITemplateNode node)
		{
			this.builder.Append("{{");
			ArgumentNullException.ThrowIfNull(node);
			node.TitleNodes.Accept(this);
			foreach (var param in node.Parameters)
			{
				param.Accept(this);
			}

			this.builder.Append("}}");
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