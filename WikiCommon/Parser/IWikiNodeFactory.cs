namespace RobinHood70.WikiCommon.Parser;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

/// <summary>Factory to create the various kinds of <see cref="IWikiNode"/>s, both from the parser itself and manually constructed nodes.</summary>
/// <remarks>This should always be the primary point of entry for creating new nodes.</remarks>
public interface IWikiNodeFactory
{
	#region Properties

	/// <summary>Gets the collection of tags that do not have to be closed (at all) to function correctly.</summary>
	ICollection<string> AllowMissingEndTag { get; }

	/// <summary>Gets the collection of tags which should be parsed as ignored ITagNodes (i.e., where there's valid wikitext inside of them).</summary>
	ICollection<string> ParsedTags { get; }

	/// <summary>Gets the collection of tags which are not parsed into wikitext.</summary>
	ICollection<string> UnparsedTags { get; }
	#endregion

	#region Methods

	/// <summary>Creates a new instance of an <see cref="ArgumentNode"/> class.</summary>
	/// <param name="name">The title.</param>
	/// <param name="defaultValue">The default value. May be null or an empty collection. If populated, this should preferentially be either a single ParameterNode or a collection of IWikiNodes representing the default value itself. For compatibility with MediaWiki, it can also be a list of parameter nodes, in which case, these will be added as individual entries to the <see cref="ArgumentNode.ExtraValues"/> collection.</param>
	/// <returns>A new instance of an <see cref="ArgumentNode"/> class.</returns>
	ArgumentNode ArgumentNode(IEnumerable<IWikiNode> name, IList<ParameterNode> defaultValue);

	/// <summary>Creates a new <see cref="ArgumentNode"/>  from its parts.</summary>
	/// <param name="name">The name.</param>
	/// <param name="defaultValue">The default value.</param>
	/// <returns>A new instance of an <see cref="ArgumentNode"/> class.</returns>
	ArgumentNode ArgumentNodeFromParts(string name, string defaultValue);

	/// <summary>Creates a new <see cref="ArgumentNode"/> from the provided text.</summary>
	/// <param name="wikiText">The text of the argument.</param>
	/// <returns>A new instance of an <see cref="ArgumentNode"/> class.</returns>
	/// <exception cref="ArgumentException">Thrown if the text provided does not represent a single argument (<c>{{{abc|123}}}</c>).</exception>
	ArgumentNode ArgumentNodeFromWikiText([Localizable(false)] string wikiText);

	/// <summary>Initializes a new instance of an <see cref="CommentNode"/> class.</summary>
	/// <param name="comment">The comment.</param>
	/// <returns>A new instance of an <see cref="CommentNode"/> class.</returns>
	CommentNode CommentNode(string comment);

	/// <summary>Escapes any pipes and, optionally, equals signs in the node collection.</summary>
	/// <param name="nodes">The node collection to escape.</param>
	/// <param name="escapeEquals">Whether or not to escape equals signs.</param>
	void EscapeParameterNodes(IEnumerable<IWikiNode>? nodes, bool escapeEquals);

	/// <summary>Escapes any pipes and, optionally, equals signs in the value.</summary>
	/// <param name="value">The text to escape.</param>
	/// <param name="escapeEquals">Whether or not to escape equals signs.</param>
	/// <returns>The escaped text.</returns>
	string EscapeParameterText(string? value, bool escapeEquals);

	/// <summary>Initializes a new instance of an <see cref="HeaderNode"/> class.</summary>
	/// <param name="level">The header level (number of equals signs). This must be between 1 and 6.</param>
	/// <param name="text">The text of the header.</param>
	/// <param name="comment">Any comments or whitespace that come after the closing ==.</param>
	/// <returns>A new instance of an <see cref="HeaderNode"/> class.</returns>
	HeaderNode HeaderNode(int level, [Localizable(false)] IEnumerable<IWikiNode> text, IEnumerable<IWikiNode> comment);

	/// <summary>Creates a new <see cref="HeaderNode"/> from the provided text.</summary>
	/// <param name="level">The header level (number of equals signs). This must be between 1 and 6.</param>
	/// <param name="text">The text of the header.</param>
	/// <returns>A new instance of an <see cref="HeaderNode"/> class.</returns>
	/// <remarks>If spaces are desired between the equals signs and the text, they must be provided as part of text.</remarks>
	/// <exception cref="ArgumentException">Thrown if the text provided does not represent a single header (<c>=== ABC 123 ===</c>).</exception>
	HeaderNode HeaderNodeFromParts(int level, [Localizable(false)] string text);

	/// <summary>Creates a new <see cref="HeaderNode"/> from the provided text.</summary>
	/// <param name="level">The header level (number of equals signs). This must be between 1 and 6.</param>
	/// <param name="text">The text of the header.</param>
	/// <param name="comment">The comment or whitespace at the end of the header.</param>
	/// <returns>A new instance of an <see cref="HeaderNode"/> class.</returns>
	/// <remarks>If spaces are desired between the equals signs and the text, they must be provided as part of text.</remarks>
	/// <exception cref="ArgumentException">Thrown if the text provided does not represent a single header (<c>=== ABC 123 ===</c>).</exception>
	HeaderNode HeaderNodeFromParts(int level, [Localizable(false)] string text, string comment);

	/// <summary>Creates a new <see cref="HeaderNode"/> from the provided text.</summary>
	/// <param name="wikiText">The wiki text of the header.</param>
	/// <returns>A new instance of an <see cref="HeaderNode"/> class.</returns>
	/// <exception cref="ArgumentException">Thrown if the text provided does not represent a single header (<c>=== ABC 123 ===</c>).</exception>
	HeaderNode HeaderNodeFromWikiText([Localizable(false)] string wikiText);

	/// <summary>Initializes a new instance of an <see cref="IgnoreNode"/> class.</summary>
	/// <param name="value">The value.</param>
	/// <returns>A new instance of an <see cref="IgnoreNode"/> class.</returns>
	IgnoreNode IgnoreNode(string value);

	/// <summary>Initializes a new instance of an <see cref="LinkNode"/> class.</summary>
	/// <param name="title">The title.</param>
	/// <param name="text">The display text. For image links, pipes should be included in the text.</param>
	/// <returns>A new instance of an <see cref="LinkNode"/> class.</returns>
	LinkNode LinkNode(IEnumerable<IWikiNode> title, IEnumerable<IWikiNode> text);

	/// <summary>Creates a new <see cref="LinkNode"/> from its parts.</summary>
	/// <param name="title">The link destination.</param>
	/// <returns>A new instance of an <see cref="LinkNode"/> class.</returns>
	LinkNode LinkNodeFromParts(string title);

	/// <summary>Creates a new <see cref="LinkNode"/> from its parts.</summary>
	/// <param name="title">The link destination.</param>
	/// <param name="displayText">The display text for the link.</param>
	/// <returns>A new instance of an <see cref="LinkNode"/> class.</returns>
	LinkNode LinkNodeFromParts(string title, string displayText);

	/// <summary>Creates a new <see cref="LinkNode"/> from its parts.</summary>
	/// <param name="title">The link destination.</param>
	/// <param name="parameters">The display text or, for image links, the full set of parameters.</param>
	/// <returns>A new instance of an <see cref="LinkNode"/> class.</returns>
	LinkNode LinkNodeFromParts(string title, IEnumerable<string>? parameters);

	/// <summary>Creates a new <see cref="LinkNode"/> from the provided text.</summary>
	/// <param name="wikiText">The wiki text of the link, optionally including surrounding brackets (<c>[[ns:a|b]]</c> or simply <c>ns:a|b</c>).</param>
	/// <returns>A new instance of an <see cref="LinkNode"/> class.</returns>
	/// <exception cref="ArgumentException">Thrown if the text provided does not represent a single link (e.g., <c>[[Link]]</c> or any variant thereof).</exception>
	LinkNode LinkNodeFromWikiText([Localizable(false)] string wikiText);

	/// <summary>Initializes a new instance of an <see cref="ParameterNode"/> class.</summary>
	/// <param name="name">The name.</param>
	/// <param name="value">The value.</param>
	/// <returns>A new instance of an <see cref="ParameterNode"/> class.</returns>
	ParameterNode ParameterNode(IEnumerable<IWikiNode>? name, IEnumerable<IWikiNode> value);

	/// <summary>Creates a new <see cref="ParameterNode"/> from another ParameterNode, copying surrounding whitespace from the other parameter.</summary>
	/// <param name="other">The other parameter.</param>
	/// <param name="value">The new parameter value.</param>
	/// <returns>A new instance of an <see cref="ParameterNode"/> class.</returns>
	ParameterNode ParameterNodeFromOther(ParameterNode? other, string value);

	/// <summary>Creates a new <see cref="ParameterNode"/> from another ParameterNode, copying surrounding whitespace from the other parameter.</summary>
	/// <param name="other">The other parameter.</param>
	/// <param name="name">The new parameter name.</param>
	/// <param name="value">The new parameter value.</param>
	/// <returns>A new instance of an <see cref="ParameterNode"/> class.</returns>
	ParameterNode ParameterNodeFromOther(ParameterNode? other, string? name, string value);

	/// <summary>Creates a new anonymous <see cref="ParameterNode"/> from a value.</summary>
	/// <param name="value">The value.</param>
	/// <returns>A new instance of an <see cref="ParameterNode"/> class.</returns>
	ParameterNode ParameterNodeFromParts(string value);

	/// <summary>Creates a new <see cref="ParameterNode"/> from a name and value.</summary>
	/// <param name="name">The name.</param>
	/// <param name="value">The value.</param>
	/// <returns>A new instance of an <see cref="ParameterNode"/> class.</returns>
	ParameterNode ParameterNodeFromParts(string? name, string value);

	/// <summary>Parses the specified text in <see cref="InclusionType.Raw">raw</see> mode, including any <see cref="IgnoreNode"/>s .</summary>
	/// <param name="text">The text to parse. Null values will be treated as empty strings.</param>
	/// <returns>A list of <see cref="IWikiNode"/>s with the parsed text.</returns>
	IList<IWikiNode> Parse(string? text);

	/// <summary>Parses the specified text.</summary>
	/// <param name="text">The text to parse. Null values will be treated as empty strings.</param>
	/// <param name="inclusionType">What to include or ignore when parsing text. Set to <see cref="InclusionType.Transcluded"/> to return text as if transcluded to another page; <see cref="InclusionType.CurrentPage"/> to return text as it would appear on the current page; <see cref="InclusionType.Raw"/> to return all text. In each case, any ignored text will be wrapped in an IgnoreNode.</param>
	/// <param name="strictInclusion"><see langword="true"/> if the output should omit <see cref="IgnoreNode"/>s; otherwise <see langword="false"/>.</param>
	/// <returns>A list of <see cref="IWikiNode"/>s with the parsed text.</returns>
	IList<IWikiNode> Parse(string? text, InclusionType inclusionType, bool strictInclusion);

	/// <summary>If the text provided represents a single node of the specified type, returns that node. Otherwise, throws an error.</summary>
	/// <typeparam name="T">The type of node desired.</typeparam>
	/// <param name="text">The text to parse. Null values will be treated as empty strings.</param>
	/// <param name="callerName">  The caller member name.</param>
	/// <returns>The single node of the specified type.</returns>
	/// <exception cref="ArgumentException">Thrown if there is more than one node in the collection, or the node is not of the specified type.</exception>
	T SingleNode<T>(string? text, [CallerMemberName] string callerName = "<Unknown>")
		where T : IWikiNode;

	/// <summary>Creates a new <see cref="TagNode"/> from its parts.</summary>
	/// <param name="name">The name.</param>
	/// <param name="attributes">The attributes.</param>
	/// <param name="innerText">The inner text.</param>
	/// <param name="close">The close.</param>
	/// <returns>A new instance of an <see cref="TagNode"/> class.</returns>
	TagNode TagNode(string name, string? attributes, string? innerText, string? close);

	/// <summary>Creates a new <see cref="TemplateNode"/> from its pre-parsed parts.</summary>
	/// <param name="title">The title.</param>
	/// <param name="parameters">The parameters.</param>
	/// <returns>A new instance of an <see cref="TemplateNode"/> class.</returns>
	TemplateNode TemplateNode(IEnumerable<IWikiNode> title, IList<ParameterNode> parameters);

	/// <summary>Creates a new <see cref="TemplateNode"/> from its text-based parts.</summary>
	/// <param name="title">The link destination.</param>
	/// <param name="parameters">The parameter collection.</param>
	/// <returns>A new instance of an <see cref="TemplateNode"/> class.</returns>
	TemplateNode TemplateNodeFromParts(string title, IEnumerable<(string? Name, string Value)> parameters);

	/// <summary>Creates a new <see cref="TemplateNode"/> from its text parts.</summary>
	/// <param name="title">The link destination.</param>
	/// <param name="parameters">The parameter collection.</param>
	/// <returns>A new instance of an <see cref="TemplateNode"/> class.</returns>
	TemplateNode TemplateNodeFromParts(string title, params (string?, string)[] parameters);

	/// <summary>Creates a new <see cref="TemplateNode"/> from its text-based parts.</summary>
	/// <param name="title">The link destination.</param>
	/// <param name="onePerLine">if set to <see langword="true"/>, parameters will be formatted one per line; otherwise, they will appear all on the same line.</param>
	/// <param name="parameters">The parameter collection.</param>
	/// <returns>A new instance of an <see cref="TemplateNode"/> class.</returns>
	TemplateNode TemplateNodeFromParts(string title, bool onePerLine, IEnumerable<(string? Name, string Value)> parameters);

	/// <summary>Creates a new <see cref="TemplateNode"/> from its text-based parts.</summary>
	/// <param name="title">The link destination.</param>
	/// <param name="onePerLine">if set to <see langword="true"/>, parameters will be formatted one per line; otherwise, they will appear all on the same line.</param>
	/// <param name="parameters">The parameter collection.</param>
	/// <returns>A new instance of an <see cref="TemplateNode"/> class.</returns>
	TemplateNode TemplateNodeFromParts(string title, bool onePerLine, params (string?, string)[] parameters);

	/// <summary>Creates a new <see cref="TemplateNode"/> from its text-based parts.</summary>
	/// <param name="title">The link destination.</param>
	/// <param name="onePerLine">if <see langword="true"/>, parameters will be formatted one per line; otherwise, they will appear all on the same line.</param>
	/// <param name="parameters">The parameter collection, with equals signs (if appropriate), but without pipes.</param>
	/// <returns>A new instance of an <see cref="TemplateNode"/> class.</returns>
	TemplateNode TemplateNodeFromParts(string title, bool onePerLine, IEnumerable<string>? parameters);

	/// <summary>Creates a new <see cref="TemplateNode"/> from its parts.</summary>
	/// <param name="title">The link destination.</param>
	/// <param name="onePerLine">if set to <see langword="true"/>, parameters will be formatted one per line; otherwise, they will appear all on the same line.</param>
	/// <param name="parameters">The parameter collection, with equals signs (if appropriate), but without pipes.</param>
	/// <returns>A new template node.</returns>
	TemplateNode TemplateNodeFromParts(string title, bool onePerLine, params string[] parameters);

	/// <summary>Creates a new <see cref="TemplateNode"/> from the provided text.</summary>
	/// <param name="text">The text of the template, including surrounding braces (<c>{{...}}</c>).</param>
	/// <returns>A new template node.</returns>
	/// <exception cref="ArgumentException">Thrown if the text provided does not represent a single template (e.g., <c>{{Template}}</c>, or any variant thereof).</exception>
	TemplateNode TemplateNodeFromWikiText([Localizable(false)] string text);

	/// <summary>Initializes a new instance of an <see cref="TextNode"/> class.</summary>
	/// <param name="text">The text.</param>
	/// <returns>A new instance of an <see cref="TextNode"/> class.</returns>
	TextNode TextNode(string text);
	#endregion
}