namespace RobinHood70.WikiCommon.Parser;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;

/// <summary>Builds the XML parse tree for the nodes, similar to that of Special:ExpandTemplates.</summary>
/// <remarks>While highly similar, the XML representation from this method does not precisely match Special:ExpandTemplates. This is intentional, arising from the different purposes of each.</remarks>
/// <seealso cref="IWikiNodeVisitor"/>
/// <remarks>Initializes a new instance of the <see cref="XmlVisitor"/> class.</remarks>
/// <param name="prettyPrint">if set to <see langword="true"/> pretty printing is enabled, providing text that is indented and on separate lines, as needed.</param>
public class XmlVisitor(bool prettyPrint) : IWikiNodeVisitor
{
	#region Fields
	private readonly StringBuilder builder = new();
	private readonly bool prettyPrint = prettyPrint;
	private int indent;
	#endregion

	#region Public Methods

	/// <summary>Builds the specified node or node collection into XML text.</summary>
	/// <param name="nodes">The node.</param>
	/// <returns>The XML text of the collection.</returns>
	public string Build(IEnumerable<IWikiNode> nodes)
	{
		ArgumentNullException.ThrowIfNull(nodes);
		this.builder.Clear();
		this.BuildTagOpen("root", null, false);
		foreach (var node in nodes)
		{
			node.Accept(this);
		}

		this.BuildTagClose("root");
		var retval = this.builder.ToString();
		this.builder.Clear();
		return retval;
	}
	#endregion

	#region IWikiNodeVisitor Methods

	/// <inheritdoc/>
	public void Visit(IArgumentNode argument)
	{
		ArgumentNullException.ThrowIfNull(argument);
		this
			.BuildTagOpen("tplarg", null, false)
			.BuildTag("title", null, argument.Name)
			.BuildTag("default", null, argument.DefaultValue);
		if (argument.ExtraValues != null)
		{
			foreach (var value in argument.ExtraValues)
			{
				this.Visit(value);
			}
		}

		this.BuildTagClose("tplarg");
	}

	/// <inheritdoc/>
	public void Visit(ICommentNode comment)
	{
		ArgumentNullException.ThrowIfNull(comment);
		this.BuildValueNode("comment", comment.Comment);
	}

	/// <inheritdoc/>
	public void Visit(IHeaderNode header)
	{
		ArgumentNullException.ThrowIfNull(header);
		this.BuildTag("h", new Dictionary<string, int>(StringComparer.Ordinal) { ["level"] = header.Level }, header.Title);
	}

	/// <inheritdoc/>
	public void Visit(IIgnoreNode ignore)
	{
		ArgumentNullException.ThrowIfNull(ignore);
		this.BuildValueNode("ignore", ignore.Value);
	}

	/// <inheritdoc/>
	public void Visit(ILinkNode link)
	{
		ArgumentNullException.ThrowIfNull(link);
		this
			.BuildTagOpen("link", null, false)
			.BuildTag("title", null, link.TitleNodes); // Title is always emitted, even if empty.
		if (link.Text.Count > 0)
		{
			this.BuildTag("value", null, link.Text);
		}

		this.BuildTagClose("link");
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
		this.BuildTagOpen("part", null, false);
		if (!parameter.Anonymous)
		{
			this
				.BuildTag("name", null, parameter.Name)
				.Indent();
			this.builder.Append('=');
		}
		else
		{
			this.BuildTag("name", null, null);
		}

		this
			.BuildTag("value", null, parameter.Value)
			.BuildTagClose("part");
	}

	/// <inheritdoc/>
	public void Visit(ITagNode tag)
	{
		ArgumentNullException.ThrowIfNull(tag);
		this
			.BuildTagOpen("ext", null, false)
			.BuildValueNode("name", tag.Name)
			.BuildValueNode("attr", tag.Attributes);
		if (tag.InnerText != null)
		{
			this.BuildValueNode("inner", tag.InnerText);
		}

		if (tag.Close != null)
		{
			this.BuildValueNode("close", tag.Close);
		}

		this.BuildTagClose("ext");
	}

	/// <inheritdoc/>
	public void Visit(ITemplateNode template)
	{
		ArgumentNullException.ThrowIfNull(template);
		this
			.BuildTagOpen("template", null, false)
			.BuildTag("title", null, template.TitleNodes); // Title is always emitted, even if empty.
		foreach (var part in template.Parameters)
		{
			part.Accept(this);
		}

		this.BuildTagClose("template");
	}

	/// <inheritdoc/>
	public void Visit(ITextNode text)
	{
		ArgumentNullException.ThrowIfNull(text);
		this.Indent();
		this.builder.Append(HtmlEncoder.Default.Encode(text.Text.Replace(' ', '_')));
	}
	#endregion

	#region Private Methods
	private XmlVisitor BuildTag(string name, Dictionary<string, int>? attributes, WikiNodeCollection? inner)
	{
		if (inner is null)
		{
			this.BuildTagOpen(name, attributes, true);
			return this;
		}

		this.BuildTagOpen(name, attributes, false);
		inner.Accept(this);
		this.BuildTagClose(name);
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