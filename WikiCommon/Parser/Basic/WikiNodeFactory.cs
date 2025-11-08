namespace RobinHood70.WikiCommon.Parser.Basic;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using RobinHood70.CommonCode;
using RobinHood70.WikiCommon.Parser;
using RobinHood70.WikiCommon.Properties;

/// <summary>A concrete factory for creating <see cref="IWikiNode"/>s to be added to a <see cref="WikiNodeCollection"/>.</summary>
/// <seealso cref="IWikiNodeFactory" />
public class WikiNodeFactory : IWikiNodeFactory
{
	#region Public Static Properties

	/// <summary>Gets a static <see cref="WikiNodeFactory"/> with the default property values.</summary>
	public static WikiNodeFactory DefaultInstance { get; } = new WikiNodeFactory();
	#endregion

	#region Public Properties

	/// <summary>Gets or sets the text to use when escaping equals signs.</summary>
	/// <value>The equals sign escape text.</value>
	public string EqualsEscape { get; set; } = "{{=}}";

	/// <summary>Gets or sets the text to use when escaping pipes.</summary>
	/// <value>The pipe escape text.</value>
	public string PipeEscape { get; set; } = "{{Pipe}}";
	#endregion

	#region Public Methods

	/// <inheritdoc/>
	public IArgumentNode ArgumentNodeFromParts(string name, string? defaultValue)
	{
		ArgumentNullException.ThrowIfNull(name);
		var text = "{{{" + name;
		if (defaultValue != null)
		{
			text += '|' + defaultValue;
		}

		text += "}}}";
		return this.ArgumentNodeFromWikiText(text);
	}

	/// <inheritdoc/>
	public IArgumentNode ArgumentNodeFromWikiText([Localizable(false)] string wikiText) => this.SingleNode<IArgumentNode>(wikiText);

	/// <inheritdoc/>
	public void EscapeParameterNodes(IEnumerable<IWikiNode>? nodes, bool escapeEquals)
	{
		ArgumentNullException.ThrowIfNull(nodes);
		foreach (var node in nodes)
		{
			if (node is ITextNode textNode)
			{
				textNode.Text = textNode.Text.Replace("|", this.PipeEscape, StringComparison.Ordinal);
				if (escapeEquals)
				{
					textNode.Text = textNode.Text.Replace("=", this.EqualsEscape, StringComparison.Ordinal);
				}
			}
		}
	}

	/// <inheritdoc/>
	public string EscapeParameterText(string? value, bool escapeEquals)
	{
		if (value is null)
		{
			return string.Empty;
		}

		var nodes = this.Parse(value);
		this.EscapeParameterNodes(nodes, escapeEquals);

		return nodes.ToRaw();
	}

	/// <inheritdoc/>
	public IHeaderNode HeaderNodeFromParts(int level, [Localizable(false)] string text) => this.HeaderNodeFromParts(level, text, string.Empty);

	/// <inheritdoc/>
	public IHeaderNode HeaderNodeFromParts(int level, [Localizable(false)] string text, string comment)
	{
		var headerNodes = this.Parse(text);
		var commentNodes = this.Parse(comment);
		return this.HeaderNode(level, headerNodes, commentNodes);
	}

	/// <inheritdoc/>
	public IHeaderNode HeaderNodeFromWikiText([Localizable(false)] string wikiText) => this.SingleNode<IHeaderNode>(wikiText);

	/// <inheritdoc/>
	public ILinkNode LinkNodeFromParts(string title) => this.LinkNodeFromParts(title, null as IEnumerable<string>);

	/// <inheritdoc/>
	public ILinkNode LinkNodeFromParts(string title, string displayText) => this.LinkNode(this.Parse(title), this.Parse(displayText));

	/// <inheritdoc/>
	public ILinkNode LinkNodeFromParts(string title, IEnumerable<string>? parameters) => this.LinkNodeFromParts(title, string.Join('|', parameters ?? []));

	/// <inheritdoc/>
	public ILinkNode LinkNodeFromWikiText([Localizable(false)] string wikiText) => this.SingleNode<ILinkNode>(wikiText);

	/// <inheritdoc/>
	public IParameterNode ParameterNodeFromOther(IParameterNode? other, string value)
	{
		if (other != null)
		{
			value = other.Value.CopyFormatTo(value);
		}

		return this.ParameterNodeFromParts(value);
	}

	/// <inheritdoc/>
	public IParameterNode ParameterNodeFromOther(IParameterNode? other, string? name, string value)
	{
		if (other != null)
		{
			if (name is not null && other.Name is not null)
			{
				name = other.Name.CopyFormatTo(name);
			}

			value = other.Value.CopyFormatTo(value);
		}

		return this.ParameterNodeFromParts(name, value);
	}

	/// <inheritdoc/>
	public IParameterNode ParameterNodeFromParts(string value) => this.ParameterNode(null, this.Parse(value));

	/// <inheritdoc/>
	public IParameterNode ParameterNodeFromParts(string? name, string value) => this.ParameterNode(name == null ? null : this.Parse(name), this.Parse(value));

	/// <inheritdoc/>
	public IList<IWikiNode> Parse(string? text) => this.Parse(text, InclusionType.Raw, false);

	/// <inheritdoc/>
	public IList<IWikiNode> Parse(string? text, InclusionType inclusionType, bool strictInclusion) =>
		new WikiStack(this, text, inclusionType, strictInclusion).GetNodes();

	/// <inheritdoc/>
	public T SingleNode<T>(string? text, [CallerMemberName] string callerName = "<Unknown>")
		where T : IWikiNode
	{
		var nodes = this.Parse(text);
		return nodes.Count == 1 && nodes[0] is T node
			? node
			: throw new ArgumentException(paramName: nameof(text), message: Globals.CurrentCulture(Resources.MalformedNodeText, this.GetType().Name, callerName));
	}

	/// <inheritdoc/>
	/// <remarks>
	/// <para>Due to the complexities of parsing parameter data, this method builds a string and then calls <see cref="TemplateNodeFromWikiText"/>, so that method should be called in preference to this one, if appropriate.</para>
	/// <para>Template parameters added through this method will NOT have their values escaped.</para>
	/// </remarks>
	public ITemplateNode TemplateNodeFromParts(string title, bool onePerLine, params string[] parameters) => this.TemplateNodeFromParts(title, onePerLine, parameters as IEnumerable<string>);

	/// <inheritdoc/>
	/// <remarks>
	/// <para>Due to the complexities of parsing parameter data, this method builds a string and then calls <see cref="TemplateNodeFromWikiText"/>, so that method should be called in preference to this one, if appropriate.</para>
	/// <para>Template parameters added through this method will NOT have their values escaped.</para>
	/// </remarks>
	public ITemplateNode TemplateNodeFromParts(string title, bool onePerLine, IEnumerable<string>? parameters)
	{
		ArgumentNullException.ThrowIfNull(title);
		StringBuilder sb = new();
		sb
			.Append("{{")
			.Append(title);
		if (parameters != null)
		{
			var addTrailingLine = false;
			foreach (var parameter in parameters)
			{
				if (onePerLine)
				{
					addTrailingLine = true;
					sb.Append('\n');
				}

				sb
					.Append('|')
					.Append(parameter);
			}

			if (addTrailingLine)
			{
				sb.Append('\n');
			}
		}

		sb.Append("}}");

		return this.TemplateNodeFromWikiText(sb.ToString());
	}

	/// <inheritdoc/>
	/// <remarks>
	/// <para>Due to the complexities of parsing parameter data, this method builds a string and then calls <see cref="TemplateNodeFromWikiText"/>, so that method should be called in preference to this one, if appropriate.</para>
	/// <para>Template parameters added through this method will automatically have their values escaped.</para>
	/// </remarks>
	public ITemplateNode TemplateNodeFromParts(string title, params (string?, string)[] parameters) => this.TemplateNodeFromParts(title, false, parameters as IEnumerable<(string?, string)>);

	/// <inheritdoc/>
	/// <remarks>
	/// <para>Due to the complexities of parsing parameter data, this method builds a string and then calls <see cref="TemplateNodeFromWikiText"/>, so that method should be called in preference to this one, if appropriate.</para>
	/// <para>Template parameters added through this method will automatically have their values escaped.</para>
	/// </remarks>
	public ITemplateNode TemplateNodeFromParts(string title, IEnumerable<(string? Name, string Value)> parameters) => this.TemplateNodeFromParts(title, false, parameters);

	/// <inheritdoc/>
	/// <remarks>
	/// <para>Due to the complexities of parsing parameter data, this method builds a string and then calls <see cref="TemplateNodeFromWikiText"/>, so that method should be called in preference to this one, if appropriate.</para>
	/// <para>Template parameters added through this method will automatically have their values escaped.</para>
	/// </remarks>
	public ITemplateNode TemplateNodeFromParts(string title, bool onePerLine, params (string?, string)[] parameters) => this.TemplateNodeFromParts(title, onePerLine, parameters as IEnumerable<(string?, string)>);

	/// <inheritdoc/>
	/// <remarks>
	/// <para>Due to the complexities of parsing parameter data, this method builds a string and then calls <see cref="TemplateNodeFromWikiText"/>, so that method should be called in preference to this one, if appropriate.</para>
	/// <para>Template parameters added through this method will automatically have their values escaped.</para>
	/// </remarks>
	public ITemplateNode TemplateNodeFromParts(string title, bool onePerLine, IEnumerable<(string? Name, string Value)> parameters)
	{
		ArgumentNullException.ThrowIfNull(title);
		StringBuilder sb = new();
		sb
			.Append("{{")
			.Append(title);
		if (parameters != null)
		{
			var addTrailingLine = false;
			foreach ((var name, var value) in parameters)
			{
				if (onePerLine)
				{
					addTrailingLine = true;
					sb.Append('\n');
				}

				sb.Append('|');
				if (name is not null)
				{
					sb
						.Append(name)
						.Append('=');
				}

				sb.Append(this.EscapeParameterText(value, name is null));
			}

			if (addTrailingLine)
			{
				sb.Append('\n');
			}
		}

		sb.Append("}}");

		return this.TemplateNodeFromWikiText(sb.ToString());
	}

	/// <inheritdoc/>
	public ITemplateNode TemplateNodeFromWikiText([Localizable(false)] string text) => this.SingleNode<ITemplateNode>(text);
	#endregion

	#region Public Virtual Methods

	/// <inheritdoc/>
	public virtual IArgumentNode ArgumentNode(IEnumerable<IWikiNode> name, IList<IParameterNode> defaultValue) =>
		new ArgumentNode(this, name, defaultValue);

	/// <inheritdoc/>
	public virtual ICommentNode CommentNode(string comment) =>
		new CommentNode(comment);

	/// <inheritdoc/>
	public virtual IHeaderNode HeaderNode(int level, [Localizable(false)] IEnumerable<IWikiNode> text, IEnumerable<IWikiNode> comment) => level is < 1 or > 6
		? throw new ArgumentOutOfRangeException(nameof(level))
		: new HeaderNode(this, level, text, comment);

	/// <inheritdoc/>
	public virtual IIgnoreNode IgnoreNode(string value) =>
		new IgnoreNode(value);

	/// <inheritdoc/>
	public virtual ILinkNode LinkNode(IEnumerable<IWikiNode> title, IEnumerable<IWikiNode> text) =>
		new LinkNode(this, title, text);

	/// <inheritdoc/>
	public virtual IParameterNode ParameterNode(IEnumerable<IWikiNode>? name, IEnumerable<IWikiNode> value) =>
		new ParameterNode(this, name, value);

	/// <inheritdoc/>
	public virtual ITagNode TagNode(string name, string? attributes, string? innerText, string? close) =>
		new TagNode(name, attributes, innerText, close);

	/// <inheritdoc/>
	public virtual ITemplateNode TemplateNode(IEnumerable<IWikiNode> title, IList<IParameterNode> parameters) => new TemplateNode(this, title, parameters);

	/// <inheritdoc/>
	public virtual ITextNode TextNode(string text) => new TextNode(text);
	#endregion
}