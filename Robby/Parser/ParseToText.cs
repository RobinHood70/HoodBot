namespace RobinHood70.Robby.Parser
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using RobinHood70.CommonCode;
	using RobinHood70.WikiCommon.Parser;
	using RobinHood70.WikiCommon.Parser.Basic;

	// TODO: In theory, it should be possible to use a single StringBuilder passed to each instance, but this is more complext than it appears at first glance, since sometimes strings need to be built but not added to the builder (e.g., processing {{{ {{TemplateGives1}}|Hello}}}}} where the "1" should be fully built but not added.

	/// <summary>Parses wikitext and converts what it can to plain text.</summary>
	/// <param name="context">The parsing context information.</param>
	/// <param name="stack">The template stack.</param>
	/// <remarks>Initializes a new instance of the <see cref="ParseToText"/> class.</remarks>
	public sealed class ParseToText(Context context, MagicWordFrame stack) : IWikiNodeVisitor
	{
		#region Fields
		private readonly StringBuilder builder = new();
		#endregion

		#region Public Properties

		/// <summary>Gets the parsing context.</summary>
		public Context Context => context;

		/// <summary>Gets the template stack.</summary>
		public MagicWordFrame Stack => stack;
		#endregion

		#region Public Static Methods

		/// <summary>Builds the plain text from a text string.</summary>
		/// <param name="text">The text to parse.</param>
		/// <param name="context">The context to parse with.</param>
		/// <returns>The parsed text.</returns>
		public static string Build(string text, Context context) => (text is null || text.Length == 0)
			? string.Empty
			: Build(new WikiNodeFactory().Parse(text), context);

		/// <summary>Builds the plain text from a text string.</summary>
		/// <param name="nodes">The pre-parsed text to parse.</param>
		/// <param name="context">The context to parse with.</param>
		/// <returns>The parsed text.</returns>
		public static string Build(IEnumerable<IWikiNode> nodes, Context context) => Build(nodes, context, MagicWordFrame.CreateRoot());
		#endregion

		#region IWikiNodeVisitor Methods

		/// <inheritdoc/>
		public void Visit(IArgumentNode argument)
		{
			ArgumentNullException.ThrowIfNull(argument);
			var argName = Build(argument.Name, context).Trim();
			var text = this.Stack.Parameters.TryGetValue(argName, out var paramValue)
				? paramValue
				: argument.DefaultValue is null
					? null
					: Build(argument.DefaultValue, context);
			this.builder.Append(text);
		}

		/// <inheritdoc/>
		public void Visit(ICommentNode comment)
		{
		}

		/// <inheritdoc/>
		public void Visit(IHeaderNode header)
		{
			ArgumentNullException.ThrowIfNull(header);
			var equals = new string('=', header.Level);
			var text = Build(header.Title, context, this.Stack);
			var comment = Build(header.Comment, context, this.Stack);
			this.builder.Append(string.Concat(equals, text, equals, comment));
		}

		/// <inheritdoc/>
		public void Visit(IIgnoreNode ignore)
		{
		}

		/// <inheritdoc/>
		public void Visit(ILinkNode link)
		{
			var siteLink = SiteLink.FromLinkNode(this.Context.Site, link);
			if (siteLink.Text is not null)
			{
				var text = Build(siteLink.Text, this.Context, this.Stack);
				this.builder.Append(text);
			}
		}

		/// <inheritdoc/>
		public void Visit(IEnumerable<IWikiNode> nodes)
		{
			ArgumentNullException.ThrowIfNull(nodes);
			if (this.builder.Length > 0)
			{
				throw new InvalidOperationException("Builder has been re-entered.");
			}

			foreach (var node in nodes)
			{
				node.Accept(this);
			}
		}

		/// <inheritdoc/>
		public void Visit(IParameterNode parameter)
		{
			throw new NotSupportedException("This should never be hit, since neither template nor link parsing ever call it.");
		}

		/// <inheritdoc/>
		public void Visit(ITagNode tag)
		{
			// For now, we just return the inner text under the assumption that tags will be things like <s><i><b> and so forth. More complex decision-making can be added later if needed.
			ArgumentNullException.ThrowIfNull(tag);
			this.builder.Append(tag.InnerText);
		}

		/// <inheritdoc/>
		public void Visit(ITemplateNode template)
		{
			ArgumentNullException.ThrowIfNull(template);
			var newStack = this.AddToStack(template, this.Stack);
			string? text = null;
			if (this.Context.FindMagicWordHandler(newStack) is MagicWordHandler handler)
			{
				text = handler(context, newStack);
			}

			// Not simply in an else clause since handler could be found and return null to indicate invalid syntax.
			text ??= template.ToRaw();
			this.builder.Append(text);
		}

		/// <inheritdoc/>
		public void Visit(ITextNode text)
		{
			ArgumentNullException.ThrowIfNull(text);
			this.builder.Append(text.Text);
		}
		#endregion

		#region Public Override Methods

		/// <inheritdoc/>
		public override string ToString() => this.builder.ToString();
		#endregion

		#region Private Static Methods
		private static string Build(string text, Context context, MagicWordFrame stack) => (text is null || text.Length == 0)
			? string.Empty
			: Build(new WikiNodeFactory().Parse(text), context, stack);

		private static string Build(IEnumerable<IWikiNode> nodes, Context context, MagicWordFrame stack)
		{
			if (nodes is null)
			{
				return string.Empty;
			}

			var parser = new ParseToText(context, stack);
			parser.Visit(nodes);
			return parser.ToString();
		}
		#endregion

		#region Private Methods
		private MagicWordFrame AddToStack(ITemplateNode template, MagicWordFrame parent)
		{
			var name = Build(template.TitleNodes, this.Context);
			var parameters = this.BuildParameters(template);
			return new MagicWordFrame(name, parameters, parent);
		}

		private Dictionary<string, string> BuildParameters(ITemplateNode template)
		{
			var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
			var paramNum = 1;
			foreach (var paramNode in template.Parameters)
			{
				string key;
				if (paramNode.Name is null)
				{
					key = paramNum.ToStringInvariant();
					++paramNum;
				}
				else
				{
					key = Build(paramNode.Name, this.Context, this.Stack);
				}

				var value = Build(paramNode.Value, this.Context, this.Stack);
				parameters.Add(key, value);
			}

			return parameters;
		}
		#endregion
	}
}
