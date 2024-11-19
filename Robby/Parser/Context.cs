namespace RobinHood70.Robby.Parser
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;

	#region Public Delegates
	public delegate string? MagicWordMethod(Context context, TemplateStack stack);
	#endregion

	/// <summary>A class that holds various context information for parsing. All properties except Site are optional; parsing will be skipped for any elements that rely on properties that are set to <see langword="null"/>.</summary>
	public sealed class Context
	{
		#region Fields
		private readonly MixedSensitivityDictionary<MagicWordMethod> parserFunctionResolvers = [];
		private readonly Dictionary<Title, MagicWordMethod> templateResolvers = new(TitleComparer.Instance);
		private readonly MixedSensitivityDictionary<MagicWordMethod> variableResolvers = [];
		private Title? title;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="Context"/> class.</summary>
		/// <param name="site">The site to be used for context. For best functionality, the site's MagicWords should be loaded (which is the default as of this writing).</param>
		public Context(Site site)
		{
			this.Site = site;
			this.AddParserFunctionMethod("namespace", NamespacePF);
			this.AddParserFunctionMethod("pagename", PageNamePF);

			this.AddTemplateMethod("Sic", SicTemplate);

			this.AddVariableMethod("namespace", NamespaceVar);
			this.AddVariableMethod("pagename", PageNameVar);
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the <see cref="Page"/> to use for magic words like {{PAGENAME}}.</summary>
		public Page? Page { get; init; }

		/// <summary>Gets the Site object.</summary>
		public Site Site { get; }

		/// <summary>Gets the title to use for magic words like {{PAGENAME}}.</summary>
		/// <remarks>Title does not need to be specified explicitly if <see cref="Page"/> is set.</remarks>
		public Title? Title
		{
			get => this.title ?? this.Page?.Title;
			init => this.title = value;
		}
		#endregion

		#region Public Methods
		public void AddParserFunctionMethod(string word, MagicWordMethod func)
		{
			ArgumentNullException.ThrowIfNull(word);
			ArgumentNullException.ThrowIfNull(func);
			var mw = this.Site.MagicWords[word];
			foreach (var alias in mw.Aliases)
			{
				this.parserFunctionResolvers.Add(mw.CaseSensitive, alias, func);
			}
		}

		public void AddTemplateMethod(string word, MagicWordMethod func)
		{
			ArgumentNullException.ThrowIfNull(word);
			ArgumentNullException.ThrowIfNull(func);
			var templateTitle = TitleFactory.FromUnvalidated(this.Site, MediaWikiNamespaces.Template, word);
			this.templateResolvers.Add(templateTitle, func);
		}

		public void AddVariableMethod(string word, MagicWordMethod func)
		{
			ArgumentNullException.ThrowIfNull(word);
			ArgumentNullException.ThrowIfNull(func);
			var mw = this.Site.MagicWords[word];
			foreach (var alias in mw.Aliases)
			{
				this.variableResolvers.Add(mw.CaseSensitive, alias, func);
			}
		}

		public MagicWordMethod? GetMagicWordFunction(TemplateStack stack)
		{
			ArgumentNullException.ThrowIfNull(stack);
			var name = stack.Name;

			// TODO: safesubst, subst
			if (string.Equals(name, "subst", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(name, "safesubst", StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}

			// Variable
			if (stack.FirstArgument is null &&
				stack.Parameters.Count == 0 &&
				this.variableResolvers.TryGetValue(name, out var varFunc))
			{
				return varFunc;
			}

			// TODO: msg, msgnw, raw
			if (string.Equals(name, "msg", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(name, "msgnw", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(name, "raw", StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}

			// Parser Function
			if (stack.FirstArgument is not null &&
				this.parserFunctionResolvers.TryGetValue(name, out var pfFunc))
			{
				return pfFunc;
			}

			// Template
			var templateTitle = TitleFactory.FromUnvalidated(this.Site, MediaWikiNamespaces.Template, name);
			return this.templateResolvers.TryGetValue(templateTitle, out var templateFunc)
				? templateFunc
				: null;
		}
		#endregion

		#region Private Static Methods
		private static string? NamespacePF(Context context, TemplateStack stack) =>
			stack.FirstArgument?.Length > 0
				? TitleFactory.FromUnvalidated(context.Site, stack.FirstArgument).Namespace.CanonicalName
				: string.Empty;

		private static string? NamespaceVar(Context context, TemplateStack stack) => context.Title?.Namespace.CanonicalName;

		private static string? PageNamePF(Context context, TemplateStack stack) =>
			stack.FirstArgument?.Length > 0
				? TitleFactory.FromUnvalidated(context.Site, stack.FirstArgument).PageName
				: string.Empty;

		private static string? PageNameVar(Context context, TemplateStack stack) => context.Title?.PageName;

		private static string? SicTemplate(Context context, TemplateStack stack) =>
			stack.Parameters.TryGetValue("1", out var value)
				? value
				: string.Empty;
		#endregion
	}
}
