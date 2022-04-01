namespace RobinHood70.Robby
{
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;

	/// <summary>This class includes all the static methods of creating Title objects.</summary>
	/// <remarks>This is not housed in <see cref="Title"/> itself to avoid the pseudo-inheritance that classes like Page nad FullTitle would have where, for example, FullTitle.FromValidated would actually produce a Title object.</remarks>
	public static class CreateTitle
	{
		#region Public Methods

		/// <summary>Initializes a new instance of the <see cref="TitleFactory"/> class.</summary>
		/// <param name="ns">The namespace the page is in.</param>
		/// <param name="pageName">Name of the page.</param>
		public static Title FromUnvalidated(Namespace ns, string pageName)
		{
			ns.ThrowNull();
			pageName.ThrowNull();
			pageName = ns.CapitalizePageName(WikiTextUtilities.TrimToTitle(pageName));
			return new Title(ns, pageName);
		}

		/// <summary>Initializes a new instance of the <see cref="TitleFactory"/> class.</summary>
		/// <param name="site">The site.</param>
		/// <param name="nsid">The namespace the page is in.</param>
		/// <param name="pageName">Name of the page.</param>
		public static Title FromUnvalidated(Site site, int nsid, string pageName) => FromUnvalidated(site.NotNull()[nsid], pageName);

		/// <summary>Initializes a new instance of the <see cref="TitleFactory"/> class.</summary>
		/// <param name="site">The site.</param>
		/// <param name="fullPageName">Name of the page.</param>
		public static Title FromUnvalidated(Site site, string fullPageName)
		{
			TitleFactory title = TitleFactory.Create(site.NotNull(), MediaWikiNamespaces.Main, WikiTextUtilities.TrimToTitle(fullPageName.NotNull()));
			return new Title(title);
		}

		/// <summary>Initializes a new instance of the <see cref="TitleFactory"/> class.</summary>
		/// <param name="ns">The namespace the page is in.</param>
		/// <param name="pageName">Name of the page.</param>
		public static Title FromValidated(Namespace ns, string pageName) => new(ns.NotNull(), pageName.NotNull());

		/// <summary>Initializes a new instance of the <see cref="TitleFactory"/> class.</summary>
		/// <param name="site">The site.</param>
		/// <param name="nsid">The namespace the page is in.</param>
		/// <param name="pageName">Name of the page.</param>
		public static Title FromValidated(Site site, int nsid, string pageName) => FromValidated(site.NotNull()[nsid], pageName);

		/// <summary>Initializes a new instance of the <see cref="TitleFactory"/> class.</summary>
		/// <param name="site">The site.</param>
		/// <param name="fullPageName">Name of the page.</param>
		public static Title FromValidated([NotNull][ValidatedNotNull] Site site, [NotNull][ValidatedNotNull] string fullPageName)
		{
			TitleFactory title = TitleFactory.Create(site.NotNull(), MediaWikiNamespaces.Main, fullPageName.NotNull());
			return new(title);
		}
		#endregion

	}
}
