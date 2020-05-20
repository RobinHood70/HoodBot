namespace RobinHood70.WikiCommon.Parser
{
	using System.Collections.Generic;
	using System.Globalization;
	using System.Text;
	using System.Text.Encodings.Web;
	using static CommonCode.Globals;

	/// <summary>Builds the XML parse tree for the nodes, similar to that of Special:ExpandTemplates.</summary>
	/// <remarks>While highly similar, the XML representation from this method does not precisely match Special:ExpandTemplates. This is intentional, arising from the different purposes of each.</remarks>
	/// <seealso cref="IWikiNodeVisitor"/>
	public class XmlVisitor : IWikiNodeVisitor
	{
		#region Fields
		private readonly StringBuilder builder = new StringBuilder();
		private readonly bool prettyPrint;
		private int indent;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="XmlVisitor"/> class.</summary>
		/// <param name="prettyPrint">if set to <c>true</c> pretty printing is enabled, providing text that is indented and on separate lines, as needed.</param>
		public XmlVisitor(bool prettyPrint) => this.prettyPrint = prettyPrint;
		#endregion

		#region Public Methods

		/// <summary>Builds the specified node or node collection into XML text.</summary>
		/// <param name="nodes">The node.</param>
		/// <returns>The XML text of the collection.</returns>
		public string Build(IEnumerable<IWikiNode> nodes)
		{
			ThrowNull(nodes, nameof(nodes));
			this.BuildTagOpen("root", null, false);
			foreach (var node in nodes)
			{
				node.Accept(this);
			}

			this.BuildTagClose("root");

			return this.builder.ToString();
		}
		#endregion

		#region IVisitor Methods

		/// <summary>Visits the specified <see cref="ArgumentNode"/>.</summary>
		/// <param name="node">The node.</param>
		public void Visit(ArgumentNode node)
		{
			ThrowNull(node, nameof(node));
			this
				.BuildTagOpen("tplarg", null, false)
				.BuildTag("title", null, node.Name)
				.BuildTag("default", null, node.DefaultValue);
			foreach (var value in node.ExtraValues)
			{
				this.BuildTag("extra", null, value);
			}

			this.BuildTagClose("tplarg");
		}

		/// <summary>Visits the specified <see cref="CommentNode"/>.</summary>
		/// <param name="node">The node.</param>
		public void Visit(CommentNode node)
		{
			ThrowNull(node, nameof(node));
			this.BuildValueNode("comment", node.Comment);
		}

		/// <summary>Visits the specified <see cref="HeaderNode"/>.</summary>
		/// <param name="node">The node.</param>
		public void Visit(HeaderNode node)
		{
			ThrowNull(node, nameof(node));
			this.BuildTag("h", new Dictionary<string, int> { ["level"] = node.Level, ["i"] = node.Index }, node.Title);
		}

		/// <summary>Visits the specified <see cref="IgnoreNode"/>.</summary>
		/// <param name="node">The node.</param>
		public void Visit(IgnoreNode node)
		{
			ThrowNull(node, nameof(node));
			this.BuildValueNode("ignore", node.Value);
		}

		/// <summary>Visits the specified <see cref="LinkNode"/>.</summary>
		/// <param name="node">The node.</param>
		public void Visit(LinkNode node)
		{
			ThrowNull(node, nameof(node));
			this
				.BuildTagOpen("link", null, false)
				.BuildTag("title", null, node.Title); // Title is always emitted, even if empty.
			foreach (var part in node.Parameters)
			{
				part.Accept(this);
			}

			this.BuildTagClose("link");
		}

		/// <summary>Visits the specified <see cref="NodeCollection"/>.</summary>
		/// <param name="nodes">The node collection.</param>
		public void Visit(NodeCollection nodes)
		{
			ThrowNull(nodes, nameof(nodes));
			foreach (var node in nodes)
			{
				node.Accept(this);
			}
		}

		/// <summary>Visits the specified <see cref="ParameterNode"/>.</summary>
		/// <param name="node">The node.</param>
		public void Visit(ParameterNode node)
		{
			ThrowNull(node, nameof(node));
			this.BuildTagOpen("part", null, false);
			if (!node.Anonymous)
			{
				this
					.BuildTag("name", null, node.Name)
					.Indent();
				this.builder.Append('=');
			}
			else
			{
				this.BuildTag("name", null, null);
			}

			this
				.BuildTag("value", null, node.Value)
				.BuildTagClose("part");
		}

		/// <summary>Visits the specified <see cref="TagNode"/>.</summary>
		/// <param name="node">The node.</param>
		public void Visit(TagNode node)
		{
			ThrowNull(node, nameof(node));
			this
				.BuildTagOpen("ext", null, false)
				.BuildValueNode("name", node.Name)
				.BuildValueNode("attr", node.Attributes);
			if (node.InnerText != null)
			{
				this.BuildValueNode("inner", node.InnerText);
			}

			if (node.Close != null)
			{
				this.BuildValueNode("close", node.Close);
			}

			this.BuildTagClose("ext");
		}

		/// <summary>Visits the specified <see cref="TemplateNode"/>.</summary>
		/// <param name="node">The node.</param>
		public void Visit(TemplateNode node)
		{
			ThrowNull(node, nameof(node));
			this
				.BuildTagOpen("template", null, false)
				.BuildTag("title", null, node.Title); // Title is always emitted, even if empty.
			foreach (var part in node.Parameters)
			{
				part.Accept(this);
			}

			this.BuildTagClose("template");
		}

		/// <summary>Visits the specified <see cref="TextNode"/>.</summary>
		/// <param name="node">The node.</param>
		public void Visit(TextNode node)
		{
			ThrowNull(node, nameof(node));
			this.Indent();
			this.builder.Append(HtmlEncoder.Default.Encode(node.Text.Replace(' ', '_')));
		}
		#endregion

		#region Private Methods
		private XmlVisitor BuildTag(string name, Dictionary<string, int>? attributes, NodeCollection? inner)
		{
			var selfClosed = inner == null || inner.Count == 0;
			this.BuildTagOpen(name, attributes, selfClosed);
			if (!selfClosed)
			{
				foreach (var node in inner!)
				{
					node.Accept(this);
				}

				this.BuildTagClose(name);
			}

			return this;
		}

		private XmlVisitor BuildTagClose(string name)
		{
			this.indent--;
			this.Indent();
			this.builder.Append("</").Append(name).Append(">\n");

			return this;
		}

		private XmlVisitor BuildTagOpen(string name, Dictionary<string, int>? attributes, bool selfClosed)
		{
			this.Indent();
			this.builder.Append('<').Append(name);
			if (attributes != null)
			{
				foreach (var kvp in attributes)
				{
					this.builder.Append(' ').Append(kvp.Key).Append("=\"").Append(kvp.Value.ToString(CultureInfo.InvariantCulture)).Append('"');
				}
			}

			if (selfClosed)
			{
				this.builder.Append("/>");
			}
			else
			{
				this.builder.Append(">\n");
				this.indent++;
			}

			return this;
		}

		private XmlVisitor BuildValueNode(string name, string? value)
		{
			var encodedValue = HtmlEncoder.Default.Encode(value) ?? string.Empty;
			this.Indent();
			this.builder
				.Append('<')
				.Append(name)
				.Append('>')
				.Append(encodedValue)
				.Append("</")
				.Append(name)
				.Append('>');

			return this;
		}

		private void Indent()
		{
			if (this.prettyPrint)
			{
				if (this.builder.Length > 0 && this.builder[^1] != '\n')
				{
					this.builder.Append('\n');
				}

				this.builder.Append(new string('\t', this.indent));
			}
		}
		#endregion
	}
}