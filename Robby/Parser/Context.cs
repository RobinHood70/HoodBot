namespace RobinHood70.Robby.Parser
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;

	#region Global Delegates
	public delegate string? TemplateFunction(string name, string? firstArg, IDictionary<string, string> parameters, Context context);
	#endregion

	/// <summary>A class that holds various context information for parsing. All properties except Site are optional; parsing will be skipped for any elements that rely on properties that are set to <see langword="null"/>.</summary>
	public sealed class Context
	{
		#region Fields
		private readonly MixedSensitivityDictionary<TemplateFunction> parserFunctionResolvers = [];
		private readonly Dictionary<Title, TemplateFunction> templateResolvers = new(TitleComparer.Instance);
		private readonly MixedSensitivityDictionary<TemplateFunction> variableResolvers = [];
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="Context"/> class.</summary>
		/// <param name="site">The site to be used for context. For best functionality, the site's MagicWords should be loaded (which is the default as of this writing).</param>
		public Context(Site site)
		{
			this.Site = site;
			this.AddMagicWord(true, "namespace", NamespacePF);
			this.AddMagicWord(false, "namespace", NamespaceVar);
			this.AddMagicWord(true, "pagename", PageNamePF);
			this.AddMagicWord(false, "pagename", PageNameVar);
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the Site object.</summary>
		public Site Site { get; }

		/// <summary>Gets the object to use for parsing {{PAGENAME}} and similar variables. Note that this can be any type of <see cref="ITitle"/>, with a <see cref="Page"/> providing the most context information to the parser.</summary>
		public ITitle? Title { get; init; }
		#endregion

		#region Public Methods
		public void AddMagicWord(bool isParserFunction, string word, TemplateFunction func)
		{
			var dict = isParserFunction ? this.parserFunctionResolvers : this.variableResolvers;
			var mw = this.Site.MagicWords[word];
			foreach (var alias in mw.Aliases)
			{
				dict.Add(mw.CaseSensitive, alias, func);
			}
		}

		public TemplateFunction? GetMagicWordFunction(string name, bool? hasArgs)
		{
			// TODO: safesubst, subst
			if (string.Equals(name, "subst", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(name, "safesubst", StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}

			// Variable
			if (hasArgs != true &&
				this.variableResolvers.TryGetValue(name, out var value))
			{
				return value;
			}

			// TODO: msg, msgnw, raw
			if (string.Equals(name, "msg", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(name, "msgnw", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(name, "raw", StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}

			// Parser Function
			if (hasArgs != false && this.parserFunctionResolvers.TryGetValue(name, out value))
			{
				return value;
			}

			// Template
			var title = TitleFactory.FromUnvalidated(this.Site, MediaWikiNamespaces.Template, name);
			this.templateResolvers.TryGetValue(title, out value);
			return value;
		}
		#endregion

		#region Private Static Methods
		private static string? NamespacePF(string name, string? firstArg, IDictionary<string, string> parameters, Context context) =>
			firstArg is null || firstArg.Length == 0
				? string.Empty
				: TitleFactory.FromUnvalidated(context.Site, firstArg).Namespace.CanonicalName;

		private static string? NamespaceVar(string name, string? firstArg, IDictionary<string, string> parameters, Context context) => context.Title?.Title.PageName;

		private static string? PageNamePF(string name, string? firstArg, IDictionary<string, string> parameters, Context context) =>
			firstArg is null || firstArg.Length == 0
				? string.Empty
				: TitleFactory.FromUnvalidated(context.Site, firstArg).Namespace.CanonicalName;

		private static string? PageNameVar(string name, string? firstArg, IDictionary<string, string> parameters, Context context) => context.Title?.Title.PageName;
		#endregion
	}
}
