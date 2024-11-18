namespace RobinHood70.Robby.Parser
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using RobinHood70.CommonCode;
	using RobinHood70.WikiCommon.Parser;
	using RobinHood70.WikiCommon.Parser.Basic;

	/// <summary>
	/// Parses wikitext and converts what it can to plain text.
	/// </summary>
	/// <remarks>Initializes a new instance of the <see cref="ParseToText"/> class.</remarks>
	/// <param name="context">The parsing context information.</param>
	public sealed class ParseToText(Context context) : IWikiNodeVisitor
	{
		#region Fields
		private readonly StringBuilder builder = new();
		#endregion

		#region Public Properties
		public Context Context { get; } = context;

		public bool ResolveLinks { get; set; }

		public string Text => this.builder.ToString();
		#endregion

		#region Public Static Methods
		public static string Build(string text, Context context) => (text is null || text.Length == 0)
			? string.Empty
			: Build(new WikiNodeFactory().Parse(text), context);

		public static string Build(IEnumerable<IWikiNode> nodes, Context context)
		{
			if (nodes is null)
			{
				return string.Empty;
			}

			var parser = new ParseToText(context);
			parser.Visit(nodes);
			return parser.Text;
		}
		#endregion

		#region IWikiNodeVisitor Methods

		/// <inheritdoc/>
		public void Visit(IArgumentNode argument)
		{
			throw new System.NotImplementedException();
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
			throw new System.NotImplementedException();
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
			var resolvedName = Build(template.TitleNodes, this.Context);
			var split = resolvedName.Split(TextArrays.Colon, 2);
			if (this.Context.GetMagicWordFunction(split[0], split.Length == 2) is TemplateFunction func)
			{
				var firstArg = split.Length == 2 ? split[1] : null;
				Dictionary<string, string> parameters = this.BuildParameters(template);
				var text = func(split[0], firstArg, parameters, this.Context) ?? template.ToRaw();
				this.builder.Append(text);
			}
			else
			{
				this.builder.Append(template.ToRaw());
			}
		}

		/// <inheritdoc/>
		public void Visit(ITextNode text)
		{
			ArgumentNullException.ThrowIfNull(text);
			this.builder.Append(text.Text);
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
					key = ParseToText.Build(paramNode.Name, this.Context);
				}

				var value = ParseToText.Build(paramNode.Value, this.Context);
				parameters.Add(key, value);
			}

			return parameters;
		}
		#endregion
	}
}
