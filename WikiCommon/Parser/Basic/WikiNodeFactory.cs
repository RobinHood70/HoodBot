namespace RobinHood70.WikiCommon.Parser.Basic
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Runtime.CompilerServices;
	using System.Text;
	using RobinHood70.CommonCode;
	using RobinHood70.WikiCommon.Parser;
	using RobinHood70.WikiCommon.Properties;

	/// <summary>A concrete factory for creating <see cref="IWikiNode"/>s to be added to a <see cref="NodeCollection"/>.</summary>
	/// <seealso cref="IWikiNodeFactory" />
	public class WikiNodeFactory : IWikiNodeFactory
	{
		#region Public Static Properties

		/// <summary>Gets or sets the text to use when escaping equals signs.</summary>
		/// <value>The equals sign escape text.</value>
		public static string EqualsEscape { get; set; } = "&#61;";

		/// <summary>Gets or sets the text to use when escaping pipes.</summary>
		/// <value>The pipe escape text.</value>
		public static string PipeEscape { get; set; } = "{{!}}";
		#endregion

		#region Public Static Methods

		/// <summary>Escapes any pipes and equals signs in the value.</summary>
		/// <param name="value">The text to escape.</param>
		/// <returns>The escaped text.</returns>
		public static string EscapeParameterName(string? value) => EscapeParameterText(value, false);

		/// <summary>Escapes any pipes and equals signs in the value.</summary>
		/// <param name="value">The text to escape.</param>
		/// <returns>The escaped text.</returns>
		public static string EscapeParameterValue(string? value) => EscapeParameterText(value, true);

		/// <summary>Escapes any pipes and, optionally, equals signs in the value.</summary>
		/// <param name="value">The text to escape.</param>
		/// <param name="escapeEquals">if set to <see langword="true"/>, equals signs are escaped as well as pipes (i.e., <c>key=value|value</c> becomes <c>key&#61;value{{!}}value</c>); otherwise, only pipes will be escaped (i.e., <c>key=value|value</c> becomes <c>key=value{{!}}value</c>).</param>
		/// <returns>The escaped text.</returns>
		public static string EscapeParameterText(string? value, bool escapeEquals)
		{
			if (value == null)
			{
				return string.Empty;
			}

			// Because we're not returning a NodeCollection, the default factory does everything we need here.
			WikiNodeFactory factory = new();
			var nodes = factory.Parse(value);
			foreach (var node in nodes)
			{
				if (node is TextNode textNode)
				{
					textNode.Text = textNode.Text.Replace("|", PipeEscape, StringComparison.Ordinal);
					if (escapeEquals)
					{
						textNode.Text = textNode.Text.Replace("=", EqualsEscape, StringComparison.Ordinal);
					}
				}
			}

			return nodes.ToRaw();
		}
		#endregion

		#region Public Methods

		/// <summary>Creates a new <see cref="IArgumentNode"/>  from its parts.</summary>
		/// <param name="name">The name.</param>
		/// <param name="defaultValue">The default value.</param>
		/// <returns>A new argument node.</returns>
		public IArgumentNode ArgumentNodeFromParts(string name, string? defaultValue)
		{
			var text = "{{{" + name.NotNull(nameof(name));
			if (defaultValue != null)
			{
				text += '|' + defaultValue;
			}

			text += "}}}";
			return this.ArgumentNodeFromWikiText(text);
		}

		/// <summary>Creates a new <see cref="IArgumentNode"/> from the provided text.</summary>
		/// <param name="wikiText">The text of the argument.</param>
		/// <returns>A new argument node.</returns>
		/// <exception cref="ArgumentException">Thrown if the text provided does not represent a single argument (<c>{{{abc|123}}}</c>).</exception>
		public IArgumentNode ArgumentNodeFromWikiText([Localizable(false)] string wikiText) => this.SingleNode<IArgumentNode>(wikiText);

		/// <summary>Creates a new <see cref="IHeaderNode"/> from the provided text.</summary>
		/// <param name="level">The header level (number of equals signs).</param>
		/// <param name="text">The text of the header.</param>
		/// <returns>A new header node.</returns>
		/// <remarks>If spaces are desired between the equals signs and the text, they must be provided as part of text.</remarks>
		/// <exception cref="ArgumentException">Thrown if the text provided does not represent a single header (<c>=== ABC 123 ===</c>).</exception>
		public IHeaderNode HeaderNodeFromParts(int level, [Localizable(false)] string text)
		{
			var equals = new string('=', level);
			return this.HeaderNodeFromWikiText(equals + text + equals);
		}

		/// <summary>Creates a new <see cref="IHeaderNode"/> from the provided text.</summary>
		/// <param name="wikiText">The wiki text of the header.</param>
		/// <returns>A new header node.</returns>
		/// <exception cref="ArgumentException">Thrown if the text provided does not represent a single header (<c>=== ABC 123 ===</c>).</exception>
		public IHeaderNode HeaderNodeFromWikiText([Localizable(false)] string wikiText) => this.SingleNode<IHeaderNode>(wikiText);

		/// <summary>Creates a new <see cref="ILinkNode"/> from its parts.</summary>
		/// <param name="title">The link destination.</param>
		/// <returns>A new link node.</returns>
		public ILinkNode LinkNodeFromParts(string title) => this.LinkNodeFromParts(title, null as IEnumerable<string>);

		/// <summary>Creates a new <see cref="ILinkNode"/> from its parts.</summary>
		/// <param name="title">The link destination.</param>
		/// <param name="displayText">The display text for the link.</param>
		/// <returns>A new link node.</returns>
		public ILinkNode LinkNodeFromParts(string title, string displayText) => this.LinkNodeFromParts(title, new[] { displayText });

		/// <summary>Creates a new <see cref="ILinkNode"/> from its parts.</summary>
		/// <param name="title">The link destination.</param>
		/// <param name="parameters">The default value.</param>
		/// <returns>A new link node.</returns>
		public ILinkNode LinkNodeFromParts(string title, IEnumerable<string>? parameters)
		{
			var titleNodes = this.Parse(title.NotNull(nameof(title)));
			List<IParameterNode> paramEntries = new();
			if (parameters != null)
			{
				foreach (var parameter in parameters)
				{
					var paramNode = this.ParameterNodeFromParts(parameter);
					paramEntries.Add(paramNode);
				}
			}

			return this.LinkNode(titleNodes, paramEntries);
		}

		/// <summary>Creates a new <see cref="ILinkNode"/> from the provided text.</summary>
		/// <param name="wikiText">The wiki text of the link, optionally including surrounding brackets (<c>[[...]]</c>).</param>
		/// <returns>A new link node.</returns>
		/// <exception cref="ArgumentException">Thrown if the text provided does not represent a single link (e.g., <c>[[Link]]</c>, or any variant thereof).</exception>
		public ILinkNode LinkNodeFromWikiText([Localizable(false)] string wikiText) => this.SingleNode<ILinkNode>(wikiText);

		/// <summary>Creates a new <see cref="IParameterNode"/> from another IParameterNode, copying surrounding whitespace from the other parameter.</summary>
		/// <param name="other">The other parameter.</param>
		/// <param name="value">The new parameter value.</param>
		/// <returns>A new, formatted <see cref="IParameterNode"/> with the specified value.</returns>
		public IParameterNode ParameterNodeFromOther(IParameterNode? other, string value)
		{
			if (other != null)
			{
				value = AddWhitespace(other.Value, value);
			}

			return this.ParameterNodeFromParts(value);
		}

		/// <summary>Creates a new <see cref="IParameterNode"/> from another IParameterNode, copying surrounding whitespace from the other parameter.</summary>
		/// <param name="other">The other parameter.</param>
		/// <param name="name">The new parameter name.</param>
		/// <param name="value">The new parameter value.</param>
		/// <returns>A new, formatted <see cref="IParameterNode"/> with the specified name and value.</returns>
		public IParameterNode ParameterNodeFromOther(IParameterNode? other, string name, string value)
		{
			if (other != null)
			{
				name = AddWhitespace(other.Name, name);
				value = AddWhitespace(other.Value, value);
			}

			return this.ParameterNodeFromParts(name, value);
		}

		/// <summary>Creates a new anonymous <see cref="IParameterNode"/> from a value.</summary>
		/// <param name="value">The value.</param>
		/// <returns>A new ParameterNode.</returns>
		public IParameterNode ParameterNodeFromParts(string value) => this.ParameterNode(null, this.Parse(value));

		/// <summary>Creates a new <see cref="IParameterNode"/> from a name and value.</summary>
		/// <param name="name">The name.</param>
		/// <param name="value">The value.</param>
		/// <returns>A new ParameterNode.</returns>
		public IParameterNode ParameterNodeFromParts(string name, string? value) => this.ParameterNode(this.Parse(name), this.Parse(value));

		/// <summary>Parses the specified text.</summary>
		/// <param name="text">The text to parse.</param>
		/// <returns>A <see cref="NodeCollection"/> with the parsed text.</returns>
		public NodeCollection Parse(string? text) => this.Parse(text, InclusionType.Raw, false);

		/// <summary>Parses the specified text.</summary>
		/// <param name="text">The text to parse.</param>
		/// <param name="inclusionType">What to include or ignore when parsing text.</param>
		/// <param name="strictInclusion"><see langword="true"/> if the output should exclude IgnoreNodes; otherwise <see langword="false"/>.</param>
		/// <returns>A <see cref="NodeCollection"/> with the parsed text.</returns>
		public NodeCollection Parse(string? text, InclusionType inclusionType, bool strictInclusion)
		{
			WikiStack stack = new(this, text, inclusionType, strictInclusion);
			return new NodeCollection(this, stack.GetNodes());
		}

		/// <summary>If the text provided represents a single node of the specified type, returns that node. Otherwise, throws an error.</summary>
		/// <typeparam name="T">The type of node desired.</typeparam>
		/// <param name="text">The text to parse.</param>
		/// <param name="callerName">  The caller member name.</param>
		/// <returns>The single node of the specified type.</returns>
		/// <exception cref="ArgumentException">Thrown if there is more than one node in the collection, or the node is not of the specified type.</exception>
		public T SingleNode<T>(string text, [CallerMemberName] string callerName = "<Unknown>")
			where T : IWikiNode
		{
			var nodes = this.Parse(text);
			return nodes.Count == 1 && nodes[0] is T node
				? node
				: throw new ArgumentException(paramName: nameof(text), message: Globals.CurrentCulture(Resources.MalformedNodeText, this.GetType().Name, callerName));
		}

		/// <summary>Creates a new TemplateNode from its parts.</summary>
		/// <param name="title">The link destination.</param>
		/// <param name="onePerLine">if set to <see langword="true"/>, parameters will be formatted one per line; otherwise, they will appear all on the same line.</param>
		/// <param name="parameters">The parameter collection, with equals signs (if appropriate), but without pipes.</param>
		/// <returns>A new template node.</returns>
		public ITemplateNode TemplateNodeFromParts(string title, bool onePerLine, params string[] parameters) => this.TemplateNodeFromParts(title, onePerLine, parameters as IEnumerable<string>);

		/// <summary>Creates a new TemplateNode from its parts.</summary>
		/// <param name="title">The link destination.</param>
		/// <param name="onePerLine">if set to <see langword="true"/>, parameters will be formatted one per line; otherwise, they will appear all on the same line.</param>
		/// <param name="parameters">The parameter collection, with equals signs (if appropriate), but without pipes.</param>
		/// <returns>A new template node.</returns>
		/// <remarks>
		/// <para>Due to the complexities of parsing parameter data, this method builds a string and then calls <see cref="TemplateNodeFromWikiText"/>, so that method should be called in favour of this one, if appropriate.</para>
		/// <para>Unlike the overloads with (name, value) tuples, template parameters added through this method will not have their values escaped.</para>
		/// </remarks>
		public ITemplateNode TemplateNodeFromParts(string title, bool onePerLine, IEnumerable<string>? parameters)
		{
			title.ThrowNull(nameof(title));
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

		/// <summary>Creates a new TemplateNode from its parts.</summary>
		/// <param name="title">The link destination.</param>
		/// <param name="parameters">The parameter collection, with equals signs (if appropriate), but without pipes.</param>
		/// <returns>A new template node.</returns>
		/// <remarks>
		/// <para>Due to the complexities of parsing parameter data, this method builds a string and then calls <see cref="TemplateNodeFromWikiText"/>, so that method should be called in favour of this one, if appropriate.</para>
		/// <para>Template parameters added through this method will automatically have their values escaped.</para>
		/// </remarks>
		public ITemplateNode TemplateNodeFromParts(string title, params (string?, string)[] parameters) => this.TemplateNodeFromParts(title, false, parameters as IEnumerable<(string?, string)>);

		/// <summary>Creates a new TemplateNode from its parts.</summary>
		/// <param name="title">The link destination.</param>
		/// <param name="parameters">The parameter collection, with equals signs (if appropriate), but without pipes.</param>
		/// <returns>A new template node.</returns>
		/// <remarks>
		/// <para>Due to the complexities of parsing parameter data, this method builds a string and then calls <see cref="TemplateNodeFromWikiText"/>, so that method should be called in favour of this one, if appropriate.</para>
		/// <para>Template parameters added through this method will automatically have their values escaped.</para>
		/// </remarks>
		public ITemplateNode TemplateNodeFromParts(string title, IEnumerable<(string? Name, string Value)> parameters) => this.TemplateNodeFromParts(title, false, parameters);

		/// <summary>Creates a new TemplateNode from its parts.</summary>
		/// <param name="title">The link destination.</param>
		/// <param name="onePerLine">if set to <see langword="true"/>, parameters will be formatted one per line; otherwise, they will appear all on the same line.</param>
		/// <param name="parameters">The parameter collection, with equals signs (if appropriate), but without pipes.</param>
		/// <returns>A new template node.</returns>
		/// <remarks>
		/// <para>Due to the complexities of parsing parameter data, this method builds a string and then calls <see cref="TemplateNodeFromWikiText"/>, so that method should be called in favour of this one, if appropriate.</para>
		/// <para>Template parameters added through this method will automatically have their values escaped.</para>
		/// </remarks>
		public ITemplateNode TemplateNodeFromParts(string title, bool onePerLine, params (string?, string)[] parameters) => this.TemplateNodeFromParts(title, onePerLine, parameters as IEnumerable<(string?, string)>);

		/// <summary>Creates a new TemplateNode from its parts.</summary>
		/// <param name="title">The link destination.</param>
		/// <param name="onePerLine">if set to <see langword="true"/>, parameters will be formatted one per line; otherwise, they will appear all on the same line.</param>
		/// <param name="parameters">The parameter collection, with equals signs (if appropriate), but without pipes.</param>
		/// <returns>A new template node.</returns>
		/// <remarks>
		/// <para>Due to the complexities of parsing parameter data, this method builds a string and then calls <see cref="TemplateNodeFromWikiText"/>, so that method should be called in favour of this one, if appropriate.</para>
		/// <para>Template parameters added through this method will automatically have their values escaped.</para>
		/// </remarks>
		public ITemplateNode TemplateNodeFromParts(string title, bool onePerLine, IEnumerable<(string? Name, string Value)> parameters)
		{
			title.ThrowNull(nameof(title));
			StringBuilder sb = new();
			sb
				.Append("{{")
				.Append(title);
			if (parameters != null)
			{
				var addTrailingLine = false;
				foreach (var (name, value) in parameters)
				{
					if (onePerLine)
					{
						addTrailingLine = true;
						sb.Append('\n');
					}

					sb.Append('|');
					if (name != null)
					{
						sb
							.Append(name)
							.Append('=');
					}

					sb.Append(EscapeParameterValue(value));
				}

				if (addTrailingLine)
				{
					sb.Append('\n');
				}
			}

			sb.Append("}}");

			return this.TemplateNodeFromWikiText(sb.ToString());
		}

		/// <summary>Creates a new <see cref="ITemplateNode"/> from the provided text.</summary>
		/// <param name="text">The text of the template, including surrounding braces (<c>{{...}}</c>).</param>
		/// <returns>A new template node.</returns>
		/// <exception cref="ArgumentException">Thrown if the text provided does not represent a single template (e.g., <c>{{Template}}</c>, or any variant thereof).</exception>
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
		public virtual IHeaderNode HeaderNode(int level, [Localizable(false)] IEnumerable<IWikiNode> text) =>
			new HeaderNode(this, level, text);

		/// <inheritdoc/>
		public virtual IIgnoreNode IgnoreNode(string value) =>
			new IgnoreNode(value);

		/// <inheritdoc/>
		public virtual ILinkNode LinkNode(IEnumerable<IWikiNode> title, IList<IParameterNode> parameters) =>
			new LinkNode(this, title, parameters);

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

		#region Private Methods
		private static string AddWhitespace(NodeCollection? nodes, string value)
		{
			if (nodes != null)
			{
				var textValue = nodes.ToValue();
				var endPos = textValue.Length - 1;
				while (endPos >= 0 && char.IsWhiteSpace(textValue[endPos]))
				{
					endPos--;
				}

				endPos++;
				if (endPos < textValue.Length)
				{
					value += textValue[endPos..];
				}

				var startLength = 0;
				while (startLength < endPos && char.IsWhiteSpace(textValue[startLength]))
				{
					startLength++;
				}

				if (startLength > 0)
				{
					value = textValue.Substring(0, startLength) + value;
				}
			}

			return value;
		}
		#endregion
	}
}
