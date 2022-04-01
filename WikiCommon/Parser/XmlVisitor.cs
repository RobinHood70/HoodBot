﻿namespace RobinHood70.WikiCommon.Parser
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Text;
	using System.Text.Encodings.Web;
	using RobinHood70.CommonCode;

	/// <summary>Builds the XML parse tree for the nodes, similar to that of Special:ExpandTemplates.</summary>
	/// <remarks>While highly similar, the XML representation from this method does not precisely match Special:ExpandTemplates. This is intentional, arising from the different purposes of each.</remarks>
	/// <seealso cref="IWikiNodeVisitor"/>
	public class XmlVisitor : IWikiNodeVisitor
	{
		#region Fields
		private readonly StringBuilder builder = new();
		private readonly bool prettyPrint;
		private int indent;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="XmlVisitor"/> class.</summary>
		/// <param name="prettyPrint">if set to <c>true</c> pretty printing is enabled, providing text that is indented and on separate lines, as needed.</param>
		public XmlVisitor(bool prettyPrint)
		{
			this.prettyPrint = prettyPrint;
		}
		#endregion

		#region Public Methods

		/// <summary>Builds the specified node or node collection into XML text.</summary>
		/// <param name="nodes">The node.</param>
		/// <returns>The XML text of the collection.</returns>
		public string Build(IEnumerable<IWikiNode> nodes)
		{
			this.BuildTagOpen("root", null, false);
			foreach (var node in nodes.NotNull())
			{
				node.Accept(this);
			}

			this.BuildTagClose("root");

			return this.builder.ToString();
		}
		#endregion

		#region IVisitor Methods

		/// <inheritdoc/>
		public void Visit(IArgumentNode node)
		{
			node.ThrowNull();
			this
				.BuildTagOpen("tplarg", null, false)
				.BuildTag("title", null, node.Name)
				.BuildTag("default", null, node.DefaultValue);
			if (node.ExtraValues != null)
			{
				foreach (var value in node.ExtraValues)
				{
					this.Visit(value);
				}
			}

			this.BuildTagClose("tplarg");
		}

		/// <inheritdoc/>
		public void Visit(ICommentNode node) => this.BuildValueNode("comment", node.NotNull().Comment);

		/// <inheritdoc/>
		public void Visit(IHeaderNode node)
		{
			node.ThrowNull();
			this.BuildTag("h", new Dictionary<string, int>(StringComparer.Ordinal) { ["level"] = node.Level }, node.Title);
		}

		/// <inheritdoc/>
		public void Visit(IIgnoreNode node) => this.BuildValueNode("ignore", node.NotNull().Value);

		/// <inheritdoc/>
		public void Visit(ILinkNode node)
		{
			node.ThrowNull();
			this
				.BuildTagOpen("link", null, false)
				.BuildTag("title", null, node.Title); // Title is always emitted, even if empty.
			foreach (var part in node.Parameters)
			{
				part.Accept(this);
			}

			this.BuildTagClose("link");
		}

		/// <inheritdoc/>
		public void Visit(NodeCollection nodes)
		{
			foreach (var node in nodes.NotNull())
			{
				node.Accept(this);
			}
		}

		/// <inheritdoc/>
		public void Visit(IParameterNode node)
		{
			this.BuildTagOpen("part", null, false);
			if (!node.NotNull().Anonymous)
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

		/// <inheritdoc/>
		public void Visit(ITagNode node)
		{
			node.ThrowNull();
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

		/// <inheritdoc/>
		public void Visit(ITemplateNode node)
		{
			node.ThrowNull();
			this
				.BuildTagOpen("template", null, false)
				.BuildTag("title", null, node.Title); // Title is always emitted, even if empty.
			foreach (var part in node.Parameters)
			{
				part.Accept(this);
			}

			this.BuildTagClose("template");
		}

		/// <inheritdoc/>
		public void Visit(ITextNode node)
		{
			this.Indent();
			this.builder.Append(HtmlEncoder.Default.Encode(node.NotNull().Text.Replace(' ', '_')));
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

		private void BuildTagClose(string name)
		{
			this.indent--;
			this.Indent();
			this.builder
				.Append("</")
				.Append(name)
				.Append(">\n");
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
			var encodedValue = HtmlEncoder.Default.Encode(value ?? string.Empty);
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