namespace RobinHood70.Robby.Parser
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using RobinHood70.CommonCode;
	using RobinHood70.WikiCommon.Parser;
	using RobinHood70.WikiCommon.Parser.Basic;

	/// <summary>Parses wikitext and converts what it can to plain text.</summary>
	/// <param name="context">The parsing context information.</param>
	/// <param name="stack">The template stack.</param>
	/// <remarks>Initializes a new instance of the <see cref="ParseToText"/> class.</remarks>
	public sealed class ParseToText(Context context, TemplateStack stack) : IWikiNodeVisitor
	{
		#region Fields
		private readonly StringBuilder builder = new();
		#endregion

		#region Public Properties
		public Context Context => context;

		public TemplateStack Stack => stack;

		public string Text => this.builder.ToString();
		#endregion

		#region Public Static Methods
		public static string Build(string text, Context context) => (text is null || text.Length == 0)
			? string.Empty
			: Build(new WikiNodeFactory().Parse(text), context);

		public static string Build(IEnumerable<IWikiNode> nodes, Context context) => Build(nodes, context, TemplateStack.NewRoot());
		#endregion

		#region IWikiNodeVisitor Methods

		/// <inheritdoc/>
		public void Visit(IArgumentNode argument)
		{
			ArgumentNullException.ThrowIfNull(argument);
			var argName = ParseToText.Build(argument.Name, context).Trim();
			var text = this.Stack.Parameters.TryGetValue(argName, out var paramValue)
				? paramValue
				: argument.DefaultValue is null
					? null
					: ParseToText.Build(argument.DefaultValue, context);
			this.builder.Append(text);
		}

		/// <inheritdoc/>
		public void Visit(ICommentNode comment)
		{
		}

		/// <inheritdoc/>
		public void Visit(IHeaderNode header)
		{
			throw new System.NotImplementedException();
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
				var text = ParseToText.Build(siteLink.Text, this.Context, this.Stack);
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
			throw new System.NotImplementedException();
		}

		/// <inheritdoc/>
		public void Visit(ITagNode tag)
		{
			throw new System.NotImplementedException();
		}

		/// <inheritdoc/>
		public void Visit(ITemplateNode template)
		{
			ArgumentNullException.ThrowIfNull(template);
			var newStack = this.AddToStack(template, this.Stack);
			string? text = null;
			if (this.Context.GetMagicWordFunction(newStack) is MagicWordMethod func)
			{
				text = func(context, newStack);
			}

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

		#region Private Static Methods
		private static string Build(string text, Context context, TemplateStack stack) => (text is null || text.Length == 0)
			? string.Empty
			: Build(new WikiNodeFactory().Parse(text), context, stack);

		private static string Build(IEnumerable<IWikiNode> nodes, Context context, TemplateStack stack)
		{
			if (nodes is null)
			{
				return string.Empty;
			}

			var parser = new ParseToText(context, stack);
			parser.Visit(nodes);
			return parser.Text;
		}
		#endregion

		#region Private Methods
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
					key = ParseToText.Build(paramNode.Name, this.Context, this.Stack);
				}

				var value = ParseToText.Build(paramNode.Value, this.Context, this.Stack);
				parameters.Add(key, value);
			}

			return parameters;
		}

		private TemplateStack AddToStack(ITemplateNode template, TemplateStack parent)
		{
			var resolvedName = Build(template.TitleNodes, this.Context);
			var split = resolvedName.Split(TextArrays.Colon, 2);
			var firstArg = split.Length == 2 ? split[1] : null;
			var parameters = this.BuildParameters(template);
			return new TemplateStack(split[0], firstArg, parameters, parent);
		}
		#endregion
	}
}
