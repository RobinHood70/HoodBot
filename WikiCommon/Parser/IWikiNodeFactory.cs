namespace RobinHood70.WikiCommon.Parser
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Runtime.CompilerServices;

	/// <summary>Factory to create the various kinds of <see cref="IWikiNode"/>s, both from the parser itself and manually constructed nodes.</summary>
	/// <remarks>This should always be the primary point of entry for creating new nodes.</remarks>
	public interface IWikiNodeFactory
	{
		#region Methods

		/// <summary>Creates a new instance of the <see cref="ArgumentNode"/> class.</summary>
		/// <param name="name">The title.</param>
		/// <param name="defaultValue">The default value. May be null or an empty collection. If populated, this should preferentially be either a single ParameterNode or a collection of IWikiNodes representing the default value itself. For compatibility with MediaWiki, it can also be a list of parameter nodes, in which case, these will be added as individual entries to the <see cref="IArgumentNode.ExtraValues"/> collection.</param>
		/// <returns>A new instance of the <see cref="ArgumentNode"/> class.</returns>
		IArgumentNode ArgumentNode(IEnumerable<IWikiNode> name, IList<IParameterNode> defaultValue);

		/// <summary>Creates a new <see cref="IArgumentNode"/>  from its parts.</summary>
		/// <param name="name">The name.</param>
		/// <param name="defaultValue">The default value.</param>
		/// <returns>A new instance of the <see cref="ArgumentNode"/> class.</returns>
		IArgumentNode ArgumentNodeFromParts(string name, string defaultValue);

		/// <summary>Creates a new <see cref="IArgumentNode"/> from the provided text.</summary>
		/// <param name="wikiText">The text of the argument.</param>
		/// <returns>A new instance of the <see cref="ArgumentNode"/> class.</returns>
		/// <exception cref="ArgumentException">Thrown if the text provided does not represent a single argument (<c>{{{abc|123}}}</c>).</exception>
		IArgumentNode ArgumentNodeFromWikiText([Localizable(false)] string wikiText);

		/// <summary>Initializes a new instance of the <see cref="CommentNode"/> class.</summary>
		/// <param name="comment">The comment.</param>
		/// <returns>A new instance of the <see cref="CommentNode"/> class.</returns>
		ICommentNode CommentNode(string comment);

		/// <summary>Initializes a new instance of the <see cref="HeaderNode"/> class.</summary>
		/// <param name="level">The level.</param>
		/// <param name="text">The text of the header.</param>
		/// <returns>A new instance of the <see cref="HeaderNode"/> class.</returns>
		IHeaderNode HeaderNode(int level, [Localizable(false)] IEnumerable<IWikiNode> text);

		/// <summary>Creates a new <see cref="IHeaderNode"/> from the provided text.</summary>
		/// <param name="level">The header level (number of equals signs).</param>
		/// <param name="text">The text of the header.</param>
		/// <returns>A new instance of the <see cref="HeaderNode"/> class.</returns>
		/// <exception cref="ArgumentException">Thrown if the text provided does not represent a single header (<c>=== ABC 123 ===</c>).</exception>
		IHeaderNode HeaderNodeFromParts(int level, [Localizable(false)] string text);

		/// <summary>Creates a new <see cref="IHeaderNode"/> from the provided text.</summary>
		/// <param name="wikiText">The wiki text of the header.</param>
		/// <returns>A new instance of the <see cref="HeaderNode"/> class.</returns>
		/// <exception cref="ArgumentException">Thrown if the text provided does not represent a single header (<c>=== ABC 123 ===</c>).</exception>
		IHeaderNode HeaderNodeFromWikiText([Localizable(false)] string wikiText);

		/// <summary>Initializes a new instance of the <see cref="IgnoreNode"/> class.</summary>
		/// <param name="value">The value.</param>
		/// <returns>A new instance of the <see cref="IgnoreNode"/> class.</returns>
		IIgnoreNode IgnoreNode(string value);

		/// <summary>Initializes a new instance of the <see cref="LinkNode"/> class.</summary>
		/// <param name="title">The title.</param>
		/// <param name="parameters">The parameters.</param>
		/// <returns>A new instance of the <see cref="LinkNode"/> class.</returns>
		ILinkNode LinkNode(IEnumerable<IWikiNode> title, IList<IParameterNode> parameters);

		/// <summary>Creates a new <see cref="ILinkNode"/> from its parts.</summary>
		/// <param name="title">The link destination.</param>
		/// <returns>A new instance of the <see cref="LinkNode"/> class.</returns>
		ILinkNode LinkNodeFromParts(string title);

		/// <summary>Creates a new <see cref="ILinkNode"/> from its parts.</summary>
		/// <param name="title">The link destination.</param>
		/// <param name="displayText">The display text for the link.</param>
		/// <returns>A new instance of the <see cref="LinkNode"/> class.</returns>
		ILinkNode LinkNodeFromParts(string title, string displayText);

		/// <summary>Creates a new <see cref="ILinkNode"/> from its parts.</summary>
		/// <param name="title">The link destination.</param>
		/// <param name="parameters">The full set of parameters for an image links).</param>
		/// <returns>A new instance of the <see cref="LinkNode"/> class.</returns>
		ILinkNode LinkNodeFromParts(string title, IEnumerable<string>? parameters);

		/// <summary>Creates a new <see cref="ILinkNode"/> from the provided text.</summary>
		/// <param name="wikiText">The wiki text of the link, optionally including surrounding brackets (<c>[[...]]</c>).</param>
		/// <returns>A new instance of the <see cref="LinkNode"/> class.</returns>
		/// <exception cref="ArgumentException">Thrown if the text provided does not represent a single link (e.g., <c>[[Link]]</c>, or any variant thereof).</exception>
		ILinkNode LinkNodeFromWikiText([Localizable(false)] string wikiText);

		/// <summary>Initializes a new instance of the <see cref="ParameterNode"/> class.</summary>
		/// <param name="name">The name.</param>
		/// <param name="value">The value.</param>
		/// <returns>A new instance of the <see cref="ParameterNode"/> class.</returns>
		IParameterNode ParameterNode(IEnumerable<IWikiNode>? name, IEnumerable<IWikiNode> value);

		/// <summary>Creates a new <see cref="IParameterNode"/> from another IParameterNode, copying surrounding whitespace from the other parameter.</summary>
		/// <param name="other">The other parameter.</param>
		/// <param name="value">The new parameter value.</param>
		/// <returns>A new instance of the <see cref="ParameterNode"/> class.</returns>
		IParameterNode ParameterNodeFromOther(IParameterNode? other, string value);

		/// <summary>Creates a new <see cref="IParameterNode"/> from another IParameterNode, copying surrounding whitespace from the other parameter.</summary>
		/// <param name="other">The other parameter.</param>
		/// <param name="name">The new parameter name.</param>
		/// <param name="value">The new parameter value.</param>
		/// <returns>A new instance of the <see cref="ParameterNode"/> class.</returns>
		IParameterNode ParameterNodeFromOther(IParameterNode? other, string? name, string value);

		/// <summary>Creates a new anonymous <see cref="IParameterNode"/> from a value.</summary>
		/// <param name="value">The value.</param>
		/// <returns>A new instance of the <see cref="ParameterNode"/> class.</returns>
		IParameterNode ParameterNodeFromParts(string value);

		/// <summary>Creates a new <see cref="IParameterNode"/> from a name and value.</summary>
		/// <param name="name">The name.</param>
		/// <param name="value">The value.</param>
		/// <returns>A new instance of the <see cref="ParameterNode"/> class.</returns>
		IParameterNode ParameterNodeFromParts(string? name, string value);

		/// <summary>Parses the specified text.</summary>
		/// <param name="text">The text to parse.</param>
		/// <returns>A <see cref="NodeCollection"/> with the parsed text.</returns>
		NodeCollection Parse(string text);

		/// <summary>Parses the specified text.</summary>
		/// <param name="text">The text to parse.</param>
		/// <param name="inclusionType">What to include or ignore when parsing text.</param>
		/// <param name="strictInclusion"><see langword="true"/> if the output should exclude IgnoreNodes; otherwise <see langword="false"/>.</param>
		/// <returns>A <see cref="NodeCollection"/> with the parsed text.</returns>
		NodeCollection Parse(string text, InclusionType inclusionType, bool strictInclusion);

		/// <summary>Parses the specified text.</summary>
		/// <param name="nodes">The <see cref="NodeCollection"/> to add to.</param>
		/// <param name="text">The text to parse.</param>
		/// <param name="inclusionType">What to include or ignore when parsing text.</param>
		/// <param name="strictInclusion"><see langword="true"/> if the output should exclude IgnoreNodes; otherwise <see langword="false"/>.</param>
		public void ParseInto(NodeCollection nodes, string text, InclusionType inclusionType, bool strictInclusion);

		/// <summary>If the text provided represents a single node of the specified type, returns that node. Otherwise, throws an error.</summary>
		/// <typeparam name="T">The type of node desired.</typeparam>
		/// <param name="text">The text to parse.</param>
		/// <param name="callerName">  The caller member name.</param>
		/// <returns>The single node of the specified type.</returns>
		/// <exception cref="ArgumentException">Thrown if there is more than one node in the collection, or the node is not of the specified type.</exception>
		T SingleNode<T>(string text, [CallerMemberName] string callerName = "<Unknown>")
			where T : IWikiNode;

		/// <summary>Initializes a new instance of the <see cref="TagNode"/> class.</summary>
		/// <param name="name">The name.</param>
		/// <param name="attributes">The attributes.</param>
		/// <param name="innerText">The inner text.</param>
		/// <param name="close">The close.</param>
		/// <returns>A new instance of the <see cref="TagNode"/> class.</returns>
		ITagNode TagNode(string name, string? attributes, string? innerText, string? close);

		/// <summary>Initializes a new instance of the <see cref="TemplateNode"/> class.</summary>
		/// <param name="title">The title.</param>
		/// <param name="parameters">The parameters.</param>
		/// <returns>A new instance of the <see cref="TemplateNode"/> class.</returns>
		ITemplateNode TemplateNode(IEnumerable<IWikiNode> title, IList<IParameterNode> parameters);

		/// <summary>Creates a new TemplateNode from its parts.</summary>
		/// <param name="title">The link destination.</param>
		/// <param name="parameters">The parameter collection, with equals signs (if appropriate), but without pipes.</param>
		/// <returns>A new template node.</returns>
		/// <remarks>
		/// <para>Due to the complexities of parsing parameter data, this method builds a string and then calls <see cref="TemplateNodeFromWikiText"/>, so that method should be called in favour of this one, if appropriate.</para>
		/// <para>Template parameters added through this method will automatically have their values escaped.</para>
		/// </remarks>
		ITemplateNode TemplateNodeFromParts(string title, IEnumerable<(string? Name, string Value)> parameters);

		/// <summary>Creates a new TemplateNode from its parts.</summary>
		/// <param name="title">The link destination.</param>
		/// <param name="parameters">The parameter collection, with equals signs (if appropriate), but without pipes.</param>
		/// <returns>A new template node.</returns>
		/// <remarks>
		/// <para>Due to the complexities of parsing parameter data, this method builds a string and then calls <see cref="TemplateNodeFromWikiText"/>, so that method should be called in favour of this one, if appropriate.</para>
		/// <para>Template parameters added through this method will automatically have their values escaped.</para>
		/// </remarks>
		ITemplateNode TemplateNodeFromParts(string title, params (string?, string)[] parameters);

		/// <summary>Creates a new TemplateNode from its parts.</summary>
		/// <param name="title">The link destination.</param>
		/// <param name="onePerLine">if set to <see langword="true"/>, parameters will be formatted one per line; otherwise, they will appear all on the same line.</param>
		/// <param name="parameters">The parameter collection, with equals signs (if appropriate), but without pipes.</param>
		/// <returns>A new template node.</returns>
		ITemplateNode TemplateNodeFromParts(string title, bool onePerLine, IEnumerable<(string? Name, string Value)> parameters);

		/// <summary>Creates a new TemplateNode from its parts.</summary>
		/// <param name="title">The link destination.</param>
		/// <param name="onePerLine">if set to <see langword="true"/>, parameters will be formatted one per line; otherwise, they will appear all on the same line.</param>
		/// <param name="parameters">The parameter collection, with equals signs (if appropriate), but without pipes.</param>
		/// <returns>A new template node.</returns>
		/// <remarks>
		/// <para>Due to the complexities of parsing parameter data, this method builds a string and then calls <see cref="TemplateNodeFromWikiText"/>, so that method should be called in favour of this one, if appropriate.</para>
		/// <para>Unlike the overloads with (name, value) tuples, template parameters added through this method will not have their values escaped.</para>
		/// </remarks>
		ITemplateNode TemplateNodeFromParts(string title, bool onePerLine, IEnumerable<string>? parameters);

		/// <summary>Creates a new TemplateNode from its parts.</summary>
		/// <param name="title">The link destination.</param>
		/// <param name="onePerLine">if set to <see langword="true"/>, parameters will be formatted one per line; otherwise, they will appear all on the same line.</param>
		/// <param name="parameters">The parameter collection, with equals signs (if appropriate), but without pipes.</param>
		/// <returns>A new template node.</returns>
		/// <remarks>
		/// <para>Due to the complexities of parsing parameter data, this method builds a string and then calls <see cref="TemplateNodeFromWikiText"/>, so that method should be called in favour of this one, if appropriate.</para>
		/// <para>Template parameters added through this method will automatically have their values escaped.</para>
		/// </remarks>
		ITemplateNode TemplateNodeFromParts(string title, bool onePerLine, params (string?, string)[] parameters);

		/// <summary>Creates a new TemplateNode from its parts.</summary>
		/// <param name="title">The link destination.</param>
		/// <param name="onePerLine">if set to <see langword="true"/>, parameters will be formatted one per line; otherwise, they will appear all on the same line.</param>
		/// <param name="parameters">The parameter collection, with equals signs (if appropriate), but without pipes.</param>
		/// <returns>A new template node.</returns>
		/// <remarks>
		/// <para>Due to the complexities of parsing parameter data, this method builds a string and then calls <see cref="TemplateNodeFromWikiText"/>, so that method should be called in favour of this one, if appropriate.</para>
		/// <para>Template parameters added through this method will automatically have their values escaped.</para>
		/// </remarks>
		ITemplateNode TemplateNodeFromParts(string title, bool onePerLine, params string[] parameters);

		/// <summary>Creates a new <see cref="ITemplateNode"/> from the provided text.</summary>
		/// <param name="text">The text of the template, including surrounding braces (<c>{{...}}</c>).</param>
		/// <returns>A new template node.</returns>
		/// <exception cref="ArgumentException">Thrown if the text provided does not represent a single template (e.g., <c>{{Template}}</c>, or any variant thereof).</exception>
		ITemplateNode TemplateNodeFromWikiText([Localizable(false)] string text);

		/// <summary>Initializes a new instance of the <see cref="TextNode"/> class.</summary>
		/// <param name="text">The text.</param>
		/// <returns>A new instance of the <see cref="TextNode"/> class.</returns>
		ITextNode TextNode(string text);
		#endregion
	}
}