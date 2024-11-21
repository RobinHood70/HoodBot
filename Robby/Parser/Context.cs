namespace RobinHood70.Robby.Parser
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;

	#region Public Delegates

	/// <summary>Delegate for a method that handles any type of magic word or template.</summary>
	/// <param name="context">The context to parse the magic word under.</param>
	/// <param name="frame">The template frame.</param>
	/// <returns>A string representing the return value of the magic word or template.</returns>
	public delegate string? MagicWordHandler(Context context, MagicWordFrame frame);
	#endregion

	/// <summary>A class that holds various context information for parsing. All properties except Site are optional; parsing will be skipped for any elements that rely on properties that are set to <see langword="null"/>.</summary>
	public sealed class Context
	{
		#region Fields
		private readonly MixedSensitivityDictionary<MagicWordHandler> parserFunctionHandlers = [];
		private readonly Dictionary<Title, MagicWordHandler> templateHandlers = new(TitleComparer.Instance);
		private readonly MixedSensitivityDictionary<MagicWordHandler> variableHandlers = [];
		private Title? title;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="Context"/> class.</summary>
		/// <param name="site">The site to be used for context. For best functionality, the site's MagicWords should be loaded (which is the default as of this writing).</param>
		public Context(Site site)
		{
			this.Site = site;
			this.AddParserFunctionHandler("fullpagename", FullPageNamePF);
			this.AddParserFunctionHandler("namespace", NamespacePF);
			this.AddParserFunctionHandler("pagename", PageNamePF);

			this.AddTemplateHandler("FC", ReturnSecondParameter);
			this.AddTemplateHandler("Hover", ReturnSecondParameter);
			this.AddTemplateHandler("Huh", Ignore);
			this.AddTemplateHandler("Pipe", Pipe);
			this.AddTemplateHandler("Sic", ReturnFirstParameter);

			this.AddVariableHandler("!", Pipe);
			this.AddVariableHandler("fullpagename", FullPageNameVar);
			this.AddVariableHandler("namespace", NamespaceVar);
			this.AddVariableHandler("pagename", PageNameVar);
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

		/// <summary>Gets a list of all magic words that the parser found but was unable to handle.</summary>
		/// <remarks>The list is always fully case sensitive, since it cannot be known if these are names in template space or case-sensitive words.</remarks>
		public SortedSet<string> UnhandledMagicWords { get; } = new(StringComparer.Ordinal);
		#endregion

		#region Public Methods

		/// <summary>Adds a new parser function handler.</summary>
		/// <param name="word">The magic word associated with the parser function.</param>
		/// <param name="handler">The handler that resolves the parser function.</param>
		public void AddParserFunctionHandler(string word, MagicWordHandler handler)
		{
			ArgumentNullException.ThrowIfNull(word);
			ArgumentNullException.ThrowIfNull(handler);
			var mw = this.Site.MagicWords[word];
			foreach (var alias in mw.Aliases)
			{
				this.parserFunctionHandlers.Add(mw.CaseSensitive, alias, handler);
			}
		}

		/// <summary>Adds a new template handler.</summary>
		/// <param name="word">The template name. For templates outside template space, specify the namespace just as you would on a wiki.</param>
		/// <param name="handler">The handler that resolves the template.</param>
		public void AddTemplateHandler(string word, MagicWordHandler handler)
		{
			ArgumentNullException.ThrowIfNull(word);
			ArgumentNullException.ThrowIfNull(handler);
			var templateTitle = TitleFactory.FromUnvalidated(this.Site, MediaWikiNamespaces.Template, word);
			this.templateHandlers.Add(templateTitle, handler);
		}

		/// <summary>Adds a new variable handler.</summary>
		/// <param name="word">The magic word associated with the variable.</param>
		/// <param name="handler">The handler that resolves the variable.</param>
		public void AddVariableHandler(string word, MagicWordHandler handler)
		{
			ArgumentNullException.ThrowIfNull(word);
			ArgumentNullException.ThrowIfNull(handler);
			var mw = this.Site.MagicWords[word];
			foreach (var alias in mw.Aliases)
			{
				this.variableHandlers.Add(mw.CaseSensitive, alias, handler);
			}
		}

		/// <summary>Finds the appropriate magic word handler based on the template details found on the current level of the frame.</summary>
		/// <param name="frame">The frame that contains the template information.</param>
		/// <returns>A suitable handler or <see langword="null"/> if no handler was found.</returns>
		public MagicWordHandler? FindMagicWordHandler(MagicWordFrame frame)
		{
			ArgumentNullException.ThrowIfNull(frame);
			var name = frame.Name;

			// TODO: safesubst, subst
			if (name.OrdinalEquals("subst") ||
				name.OrdinalEquals("safesubst"))
			{
				return null;
			}

			// Variable
			if (frame.MayBeVariable && this.variableHandlers.TryGetValue(name, out var handler))
			{
				return handler;
			}

			// TODO: msg, msgnw, raw
			if (name.OrdinalEquals("msg") ||
				name.OrdinalEquals("msgnw") ||
				name.OrdinalEquals("raw"))
			{
				return null;
			}

			// Parser Function
			if (frame.IsParserFunction &&
				this.parserFunctionHandlers.TryGetValue(name, out handler))
			{
				return handler;
			}

			// Template
			var templateTitle = TitleFactory.FromUnvalidated(this.Site, MediaWikiNamespaces.Template, name);
			if (this.templateHandlers.TryGetValue(templateTitle, out handler))
			{
				return handler;
			}

			this.UnhandledMagicWords.Add(name);
			return null;
		}
		#endregion

		#region Private Static Methods
		private static string? FullPageNamePF(Context context, MagicWordFrame frame) =>
			frame.FirstArgument?.Length > 0
				? TitleFactory.FromUnvalidated(context.Site, frame.FirstArgument).FullPageName()
				: string.Empty;

		private static string? FullPageNameVar(Context context, MagicWordFrame frame) => context.Title?.FullPageName();

		private static string? Ignore(Context context, MagicWordFrame frame) => string.Empty;

		private static string? NamespacePF(Context context, MagicWordFrame frame) =>
			frame.FirstArgument?.Length > 0
				? TitleFactory.FromUnvalidated(context.Site, frame.FirstArgument).Namespace.CanonicalName
				: string.Empty;

		private static string? NamespaceVar(Context context, MagicWordFrame frame) => context.Title?.Namespace.CanonicalName;

		private static string? PageNamePF(Context context, MagicWordFrame frame) =>
			frame.FirstArgument?.Length > 0
				? TitleFactory.FromUnvalidated(context.Site, frame.FirstArgument).PageName
				: string.Empty;

		private static string? PageNameVar(Context context, MagicWordFrame frame) => context.Title?.PageName;

		private static string? Pipe(Context context, MagicWordFrame frame) => "|";

		private static string? ReturnFirstParameter(Context context, MagicWordFrame frame) =>
			frame.Parameters.TryGetValue("1", out var value)
				? value
				: string.Empty;

		private static string? ReturnSecondParameter(Context context, MagicWordFrame frame) =>
			frame.Parameters.TryGetValue("2", out var value)
				? value
				: string.Empty;
		#endregion
	}
}