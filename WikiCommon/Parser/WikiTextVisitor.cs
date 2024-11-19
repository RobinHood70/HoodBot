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
			var retval = this.builder.ToString();
			this.builder.Clear();
			return retval;
		}

		/// <summary>Builds the specified node or node collection into wiki text.</summary>
		/// <param name="nodes">The nodes.</param>
		/// <returns>The wiki text of the collection.</returns>
		public string Build(IEnumerable<IWikiNode>? nodes)
		{
			if (nodes is null)
			{
				return string.Empty;
			}

			this.builder.Clear();
			this.Visit(nodes);
			var retval = this.builder.ToString();
			this.builder.Clear();
			return retval;
		}
		#endregion

		#region IVisitor Methods

		/// <inheritdoc/>
		public void Visit(IArgumentNode argument)
		{
			ArgumentNullException.ThrowIfNull(argument);
			if (this.raw || argument.DefaultValue == null)
			{
				this.builder.Append("{{{");
				argument.Name.Accept(this);
				if (argument.DefaultValue != null)
				{
					this.builder.Append('|');
					argument.DefaultValue.Accept(this);
				}

				if (this.raw && argument.ExtraValues != null)
				{
					foreach (var value in argument.ExtraValues)
					{
						value.Accept(this);
					}
				}

				this.builder.Append("}}}");
			}
			else
			{
				argument.DefaultValue.Accept(this);
			}
		}

		/// <inheritdoc/>
		public void Visit(ICommentNode comment)
		{
			ArgumentNullException.ThrowIfNull(comment);
			if (this.raw)
			{
				this.builder.Append(comment.Comment);
			}
		}

		/// <inheritdoc/>
		public void Visit(IHeaderNode header)
		{
			ArgumentNullException.ThrowIfNull(header);
			var equalsSigns = new string('=', header.Level);
			this.builder.Append(equalsSigns);
			header.Title.Accept(this);
			this.builder.Append(equalsSigns);
			header.Comment?.Accept(this);
		}

		/// <inheritdoc/>
		public void Visit(IIgnoreNode ignore)
		{
			ArgumentNullException.ThrowIfNull(ignore);
			if (this.raw)
			{
				this.builder.Append(ignore.Value);
			}
		}

		/// <inheritdoc/>
		public void Visit(ILinkNode link)
		{
			ArgumentNullException.ThrowIfNull(link);
			if (this.raw)
			{
				this.builder.Append("[[");
				link.TitleNodes.Accept(this);
				foreach (var param in link.Parameters)
				{
					param.Accept(this);
				}

				this.builder.Append("]]");
			}
			else
			{
				if (link.Parameters.Count > 0)
				{
					foreach (var parameter in link.Parameters)
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
					link.TitleNodes.Accept(this);
				}
			}
		}

		/// <inheritdoc/>
		public void Visit(IEnumerable<IWikiNode> nodes)
		{
			ArgumentNullException.ThrowIfNull(nodes);
			foreach (var node in nodes)
			{
				node.Accept(this);
			}
		}

		/// <inheritdoc/>
		public void Visit(IParameterNode parameter)
		{
			ArgumentNullException.ThrowIfNull(parameter);
			this.builder.Append('|');
			if (parameter.Name is not null)
			{
				parameter.Name.Accept(this);
				this.builder.Append('=');
			}

			parameter.Value.Accept(this);
		}

		/// <inheritdoc/>
		public void Visit(ITagNode tag)
		{
			ArgumentNullException.ThrowIfNull(tag);
			if (this.raw)
			{
				this.builder
					.Append('<')
					.Append(tag.Name)
					.Append(tag.Attributes);
				if (tag.SelfClosed)
				{
					this.builder.Append("/>");
				}
				else
				{
					// TODO: Check what happens with <br> vs. <br/> vs. <br></br>. Do they all work properly?
					this.builder
						.Append('>')
						.Append(tag.InnerText)
						.Append(tag.Close);
				}
			}
		}

		/// <inheritdoc/>
		public void Visit(ITemplateNode template)
		{
			this.builder.Append("{{");
			ArgumentNullException.ThrowIfNull(template);
			template.TitleNodes.Accept(this);
			foreach (var param in template.Parameters)
			{
				param.Accept(this);
			}

			this.builder.Append("}}");
		}

		/// <inheritdoc/>
		public void Visit(ITextNode text)
		{
			ArgumentNullException.ThrowIfNull(text);
			this.builder.Append(text.Text);
		}
		#endregion
	}
}