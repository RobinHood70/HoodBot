namespace RobinHood70.WikiCommon.Parser.Basic;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using RobinHood70.CommonCode;
using RobinHood70.WikiCommon.Parser;
using RobinHood70.WikiCommon.Properties;

// CONSIDER: What needs to be done with this now that interfaces have been removed. Can these simply be replaced by new X() calls and the like instead?

/// <summary>A concrete factory for creating <see cref="IWikiNode"/>s to be added to a <see cref="WikiNodeCollection"/>.</summary>
/// <seealso cref="IWikiNodeFactory" />
public class WikiNodeFactory : IWikiNodeFactory
{
	#region Public Static Properties

	/// <summary>Gets a static <see cref="WikiNodeFactory"/> with the default property values.</summary>
	public static WikiNodeFactory DefaultInstance { get; } = new WikiNodeFactory();
	#endregion

	#region Public Properties

	/// <inheritdoc/>
	public ICollection<string> AllowMissingEndTag { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "includeonly", "noinclude", "onlyinclude" };

	/// <summary>Gets or sets the text to use when escaping equals signs.</summary>
	/// <value>The equals sign escape text.</value>
	public string EqualsEscape { get; set; } = "{{=}}";

	/// <summary>Gets the list of tags which should be parsed as ignored ITagNodes (i.e., where there's valid wikitext inside of them).</summary>
	/// <value>The tags.</value>
	public ICollection<string> ParsedTags { get; } = [];

	/// <summary>Gets or sets the text to use when escaping pipes.</summary>
	/// <value>The pipe escape text.</value>
	public string PipeEscape { get; set; } = "{{Pipe}}";

	/// <summary>Gets the list of tags which are not parsed into wikitext.</summary>
	/// <value>The unparsed tags.</value>
	public ICollection<string> UnparsedTags { get; } = ["gallery", "indicator", "nowiki", "pre"];
	#endregion

	#region Public Methods

	/// <inheritdoc/>
	public ArgumentNode ArgumentNodeFromParts(string name, string? defaultValue)
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
	public ArgumentNode ArgumentNodeFromWikiText([Localizable(false)] string wikiText) => this.SingleNode<ArgumentNode>(wikiText);

	/// <inheritdoc/>
	public void EscapeParameterNodes(IEnumerable<IWikiNode>? nodes, bool escapeEquals)
	{
		ArgumentNullException.ThrowIfNull(nodes);
		foreach (var node in nodes)
		{
			if (node is TextNode textNode)
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
	public HeaderNode HeaderNodeFromParts(int level, [Localizable(false)] string text) => this.HeaderNodeFromParts(level, text, string.Empty);

	/// <inheritdoc/>
	public HeaderNode HeaderNodeFromParts(int level, [Localizable(false)] string text, string comment)
	{
		var headerNodes = this.Parse(text);
		var commentNodes = this.Parse(comment);
		return this.HeaderNode(level, headerNodes, commentNodes);
	}

	/// <inheritdoc/>
	public HeaderNode HeaderNodeFromWikiText([Localizable(false)] string wikiText) => this.SingleNode<HeaderNode>(wikiText);

	/// <inheritdoc/>
	public LinkNode LinkNodeFromParts(string title) => this.LinkNodeFromParts(title, null as IEnumerable<string>);

	/// <inheritdoc/>
	public LinkNode LinkNodeFromParts(string title, string displayText) => this.LinkNode(this.Parse(title), this.Parse(displayText));

	/// <inheritdoc/>
	public LinkNode LinkNodeFromParts(string title, IEnumerable<string>? parameters) => this.LinkNodeFromParts(title, string.Join('|', parameters ?? []));

	/// <inheritdoc/>
	public LinkNode LinkNodeFromWikiText([Localizable(false)] string wikiText) => this.SingleNode<LinkNode>(wikiText);

	/// <inheritdoc/>
	public ParameterNode ParameterNodeFromOther(ParameterNode? other, string value)
	{
		if (other != null)
		{
			value = other.Value.CopyFormatTo(value);
		}

		return this.ParameterNodeFromParts(value);
	}

	/// <inheritdoc/>
	public ParameterNode ParameterNodeFromOther(ParameterNode? other, string? name, string value)
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
	public ParameterNode ParameterNodeFromParts(string value) => this.ParameterNode(null, this.Parse(value));

	/// <inheritdoc/>
	public ParameterNode ParameterNodeFromParts(string? name, string value) => this.ParameterNode(name == null ? null : this.Parse(name), this.Parse(value));

	/// <inheritdoc/>
	public IList<IWikiNode> Parse(string? text) => this.Parse(text, InclusionType.Raw, false);

	/// <inheritdoc/>
	public IList<IWikiNode> Parse(string? text, InclusionType inclusionType, bool strictInclusion) =>
		new WikiStack(this, text, inclusionType, strictInclusion, 4).GetNodes();

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
	public TemplateNode TemplateNodeFromParts(string title, bool onePerLine, params string[] parameters) => this.TemplateNodeFromParts(title, onePerLine, parameters as IEnumerable<string>);

	/// <inheritdoc/>
	/// <remarks>
	/// <para>Due to the complexities of parsing parameter data, this method builds a string and then calls <see cref="TemplateNodeFromWikiText"/>, so that method should be called in preference to this one, if appropriate.</para>
	/// <para>Template parameters added through this method will NOT have their values escaped.</para>
	/// </remarks>
	public TemplateNode TemplateNodeFromParts(string title, bool onePerLine, IEnumerable<string>? parameters)
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
	public TemplateNode TemplateNodeFromParts(string title, params (string?, string)[] parameters) => this.TemplateNodeFromParts(title, false, parameters as IEnumerable<(string?, string)>);

	/// <inheritdoc/>
	/// <remarks>
	/// <para>Due to the complexities of parsing parameter data, this method builds a string and then calls <see cref="TemplateNodeFromWikiText"/>, so that method should be called in preference to this one, if appropriate.</para>
	/// <para>Template parameters added through this method will automatically have their values escaped.</para>
	/// </remarks>
	public TemplateNode TemplateNodeFromParts(string title, IEnumerable<(string? Name, string Value)> parameters) => this.TemplateNodeFromParts(title, false, parameters);

	/// <inheritdoc/>
	/// <remarks>
	/// <para>Due to the complexities of parsing parameter data, this method builds a string and then calls <see cref="TemplateNodeFromWikiText"/>, so that method should be called in preference to this one, if appropriate.</para>
	/// <para>Template parameters added through this method will automatically have their values escaped.</para>
	/// </remarks>
	public TemplateNode TemplateNodeFromParts(string title, bool onePerLine, params (string?, string)[] parameters) => this.TemplateNodeFromParts(title, onePerLine, parameters as IEnumerable<(string?, string)>);

	/// <inheritdoc/>
	/// <remarks>
	/// <para>Due to the complexities of parsing parameter data, this method builds a string and then calls <see cref="TemplateNodeFromWikiText"/>, so that method should be called in preference to this one, if appropriate.</para>
	/// <para>Template parameters added through this method will automatically have their values escaped.</para>
	/// </remarks>
	public TemplateNode TemplateNodeFromParts(string title, bool onePerLine, IEnumerable<(string? Name, string Value)> parameters)
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
	public TemplateNode TemplateNodeFromWikiText([Localizable(false)] string text) => this.SingleNode<TemplateNode>(text);
	#endregion

	#region Public Virtual Methods

	/// <inheritdoc/>
	public virtual ArgumentNode ArgumentNode(IEnumerable<IWikiNode> name, IList<ParameterNode> defaultValue) =>
		new(this, name, defaultValue);

	/// <inheritdoc/>
	public virtual CommentNode CommentNode(string comment) =>
		new(comment);

	/// <inheritdoc/>
	public virtual HeaderNode HeaderNode(int level, [Localizable(false)] IEnumerable<IWikiNode> text, IEnumerable<IWikiNode> comment) => level is < 1 or > 6
		? throw new ArgumentOutOfRangeException(nameof(level))
		: new HeaderNode(this, level, text, comment);

	/// <inheritdoc/>
	public virtual IgnoreNode IgnoreNode(string value) =>
		new(value);

	/// <inheritdoc/>
	public virtual LinkNode LinkNode(IEnumerable<IWikiNode> title, IEnumerable<IWikiNode> text) =>
		new(this, title, text);

	/// <inheritdoc/>
	public virtual ParameterNode ParameterNode(IEnumerable<IWikiNode>? name, IEnumerable<IWikiNode> value) =>
		new(this, name, value);

	/// <inheritdoc/>
	public virtual TagNode TagNode(string name, string? attributes, string? innerText, string? close) =>
		new(name, attributes, innerText, close);

	/// <inheritdoc/>
	public virtual TemplateNode TemplateNode(IEnumerable<IWikiNode> title, IList<ParameterNode> parameters) =>
		new(this, title, parameters);

	/// <inheritdoc/>
	public virtual TextNode TextNode(string text) => new(text);
	#endregion
}