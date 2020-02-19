namespace RobinHood70.WikiClasses.Parser
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.WikiClasses.Properties;
	using static WikiCommon.Globals;

	/// <summary>Represents a parameter to a template or link.</summary>
	public class ParameterNode : IWikiNode
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="ParameterNode"/> class.</summary>
		/// <param name="value">The value.</param>
		public ParameterNode(IEnumerable<IWikiNode> value) => this.Value = new NodeCollection(this, value ?? throw ArgumentNull(nameof(value)));

		/// <summary>Initializes a new instance of the <see cref="ParameterNode"/> class.</summary>
		/// <param name="name">The name.</param>
		/// <param name="value">The value.</param>
		public ParameterNode(IEnumerable<IWikiNode> name, IEnumerable<IWikiNode> value)
		{
			this.Name = new NodeCollection(this, name ?? throw ArgumentNull(nameof(name)));
			this.Value = new NodeCollection(this, value ?? throw ArgumentNull(nameof(value)));
		}
		#endregion

		#region Public Static Properties

		/// <summary>Gets or sets the text to use when escaping equals signs.</summary>
		/// <value>The equals sign escape text.</value>
		public static string EqualsEscape { get; set; } = "&#61;";

		/// <summary>Gets or sets the text to use when escaping pipes.</summary>
		/// <value>The pipe escape text.</value>
		public static string PipeEscape { get; set; } = "{{!}}";
		#endregion

		#region Public Properties

		/// <summary>Gets a value indicating whether this <see cref="ParameterNode">parameter</see> is anonymous.</summary>
		/// <value><c>true</c> if anonymous; otherwise, <c>false</c>.</value>
		public bool Anonymous => this.Name == null;

		/// <summary>Gets the name of the parameter, if not anonymous.</summary>
		/// <value>The name.</value>
		public NodeCollection? Name { get; private set; }

		/// <summary>Gets an enumerator that iterates through any NodeCollections this node contains.</summary>
		/// <returns>An enumerator that can be used to iterate through additional NodeCollections.</returns>
		public IEnumerable<NodeCollection> NodeCollections
		{
			get
			{
				if (this.Name != null)
				{
					yield return this.Name;
				}

				yield return this.Value;
			}
		}

		/// <summary>Gets the parameter value.</summary>
		/// <value>The value.</value>
		public NodeCollection Value { get; }
		#endregion

		#region Public Static Methods

		/// <summary>Copies the format (surrounding whitespace) from another parameter into a new anonymous parameter.</summary>
		/// <param name="other">The other parameter.</param>
		/// <param name="value">The new parameter value.</param>
		/// <returns>A new, formatted <see cref="ParameterNode"/> with the specified value.</returns>
		public static ParameterNode CopyFormatFrom(ParameterNode? other, string value)
		{
			if (other != null)
			{
				value = AddWhitespace(other.Value, value);
			}

			return FromParts(value);
		}

		/// <summary>Copies the format (surrounding whitespace) from another parameter into a new named parameter.</summary>
		/// <param name="other">The other parameter.</param>
		/// <param name="name">The new parameter name.</param>
		/// <param name="value">The new parameter value.</param>
		/// <returns>A new, formatted <see cref="ParameterNode"/> with the specified value.</returns>
		public static ParameterNode CopyFormatFrom(ParameterNode? other, string name, string value)
		{
			if (other != null)
			{
				name = AddWhitespace(other.Name, name);
				value = AddWhitespace(other.Value, value);
			}

			return FromParts(name, value);
		}

		/// <summary>Escapes any pipes in the value.</summary>
		/// <param name="nameValue">The text to escape.</param>
		/// <returns>The escaped text.</returns>
		public static string EscapeNameValue(string? nameValue)
		{
			if (nameValue == null)
			{
				return string.Empty;
			}

			var nodes = WikiTextParser.Parse(nameValue);
			EscapeValue(nodes, false);
			return WikiTextVisitor.Raw(nodes);
		}

		/// <summary>Escapes any pipes and equals signs in the value.</summary>
		/// <param name="value">The text to escape.</param>
		/// <returns>The escaped text.</returns>
		public static string EscapeValue(string? value)
		{
			if (value == null)
			{
				return string.Empty;
			}

			var nodes = WikiTextParser.Parse(value);
			EscapeValue(nodes, true);
			return WikiTextVisitor.Raw(nodes);
		}

		/// <summary>Escapes any pipes and, optionally, equals signs in the value.</summary>
		/// <param name="nodes">The <see cref="NodeCollection"/> whose <see cref="TextNode"/>s should be escaped.</param>
		/// <param name="escapeEquals">if set to <c>true</c> equals signs are escaped as well as pipes (i.e., with the default settings, <c>key=value|value</c> becomes <c>key&#61;value{{!}}value</c>); otherwise, only pipes will be escaped (i.e., <c>key=value|value</c> becomes <c>key=value{{!}}value</c>).</param>
		/// <remarks><see cref="TextNode"/>s at the root of the collection will potentially have been modified with the replaced text. For simplicity and speed, template-like replacements (e.g., {{!}} or {{=}}) will <i>not</i> have been inserted as <see cref="TemplateNode"/>s, but only as text replacements within the TextNode itself. If correct parsing is needed, callers should convert the NodeCollection to text and then back again.</remarks>
		public static void EscapeValue(NodeCollection nodes, bool escapeEquals)
		{
			ThrowNull(nodes, nameof(nodes));
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
		}

		/// <summary>Creates a new ParameterNode from the provided text.</summary>
		/// <param name="txt">The text of the parameter (without a pipe (<c>|</c>).</param>
		/// <returns>A new ParameterNode.</returns>
		/// <remarks>Due to the way the parser works, this method internally creates a template in order to parse the parameter. If you are calling this method as part of constructing a link or template, it is faster to use their methods and construct the entire object at once.</remarks>
		public static ParameterNode FromText(string txt)
		{
			ThrowNull(txt, nameof(txt));
			var template = TemplateNode.FromParts(string.Empty, false, new[] { txt });
			return template.Parameters.Count == 1 && template.Parameters.First is LinkedListNode<IWikiNode> first && first.Value is ParameterNode retval
				? retval
				: throw new InvalidOperationException(CurrentCulture(Resources.MalformedNodeText, nameof(ParameterNode), nameof(FromText)));
		}

		/// <summary>Initializes a new instance of the <see cref="ParameterNode"/> class as an anonymous parameter.</summary>
		/// <param name="value">The value.</param>
		/// <returns>A new ParameterNode.</returns>
		public static ParameterNode FromParts(string value) => new ParameterNode(WikiTextParser.Parse(value));

		/// <summary>Initializes a new instance of the <see cref="ParameterNode"/> class.</summary>
		/// <param name="name">The name.</param>
		/// <param name="value">The value.</param>
		/// <returns>A new ParameterNode.</returns>
		public static ParameterNode FromParts(string name, string value) => new ParameterNode(WikiTextParser.Parse(name), WikiTextParser.Parse(value));
		#endregion

		#region Public Methods

		/// <summary>Accepts a visitor to process the node.</summary>
		/// <param name="visitor">The visiting class.</param>
		public void Accept(IWikiNodeVisitor visitor) => visitor?.Visit(this);

		/// <summary>Gets the parameter's name, converting anonymous parameters to their numbered value.</summary>
		/// <returns>The parameter name.</returns>
		public string? NameToText() => this.Name == null ? null : WikiTextVisitor.Value(this.Name).Trim();

		/// <summary>Sets the name from a list of nodes.</summary>
		/// <param name="name">The name. If non-null, <see cref="Index"/> will be set to zero.</param>
		/// <remarks>If the name is currently null, a new NodeCollection will be created; otherwise, the existing collection will be cleared and re-populated, so existing references to Name will remain intact.</remarks>
		public void SetName(string name)
		{
			if (name == null)
			{
				this.Name = null;
			}
			else
			{
				this.SetName(WikiTextParser.Parse(name));
			}
		}

		/// <summary>Sets the name from a list of nodes.</summary>
		/// <param name="name">The name. If non-null, <see cref="Index"/> will be set to zero.</param>
		/// <remarks>If the name is currently null, a new NodeCollection will be created; otherwise, the existing collection will be cleared and re-populated, so existing references to Name will remain intact.</remarks>
		public void SetName(IEnumerable<IWikiNode>? name)
		{
			if (name == null)
			{
				this.Name = null;
			}
			else
			{
				if (this.Name == null)
				{
					this.Name = new NodeCollection(this, name);
				}
				else
				{
					this.Name.Clear();
					this.Name.AddRange(name);
				}
			}
		}
		#endregion

		#region Public Override Methods

		/// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
		/// <returns>A <see cref="string"/> that represents this instance.</returns>
		public override string ToString()
		{
			// For simple name=value nodes, display the text; otherwise, display "name" and/or "value" as needed so we're not executing time-consuming processing here.
			var name =
				this.Anonymous
					? string.Empty :
				this.Name?.Count == 1 && this.Name.First is LinkedListNode<IWikiNode> firstName && firstName.Value is TextNode nameNode
					? nameNode.Text
					: "<name>";
			var value = this.Value.Count == 1 && this.Value.First is LinkedListNode<IWikiNode> firstValue && firstValue.Value is TextNode valueNode
				? valueNode.Text
				: "<value>";
			return $"|{name}={value}";
		}
		#endregion

		#region Private Methods
		private static string AddWhitespace(NodeCollection? nodes, string value)
		{
			if (nodes != null)
			{
				var textValue = WikiTextVisitor.Value(nodes);
				var startLength = 0;
				while (startLength < textValue.Length && char.IsWhiteSpace(textValue[startLength]))
				{
					startLength++;
				}

				if (startLength > 0)
				{
					value = textValue.Substring(0, startLength) + value;
				}

				if (startLength < textValue.Length)
				{
					var endPos = textValue.Length - 1;
					while (endPos >= 0 && char.IsWhiteSpace(textValue[endPos]))
					{
						endPos--;
					}

					endPos++;
					if (endPos < textValue.Length)
					{
						value += textValue.Substring(endPos);
					}
				}
			}

			return value;
		}
		#endregion
	}
}