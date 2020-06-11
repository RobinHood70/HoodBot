namespace RobinHood70.Robby.Design
{
	using System;
	using static RobinHood70.CommonCode.Globals;

	/// <summary>Base object for Title-like objects.</summary>
	public class SimpleTitle : ISimpleTitle
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="SimpleTitle"/> class.</summary>
		/// <param name="ns">The namespace of the title.</param>
		/// <param name="pageName">The page name (without leading namespace).</param>
		public SimpleTitle(Namespace ns, string pageName)
		{
			ThrowNull(ns, nameof(ns));
			ThrowNull(pageName, nameof(pageName));
			this.Namespace = ns;
			this.PageName = pageName;
		}

		/// <summary>Initializes a new instance of the <see cref="SimpleTitle"/> class.</summary>
		/// <param name="site">The site this title is from.</param>
		/// <param name="ns">The namespace ID of the title.</param>
		/// <param name="pageName">The page name (without leading namespace).</param>
		public SimpleTitle(Site site, int ns, string pageName)
		{
			ThrowNull(site, nameof(site));
			ThrowNull(pageName, nameof(pageName));
			if (!site.Namespaces.Contains(ns))
			{
				throw new ArgumentOutOfRangeException(nameof(ns));
			}

			this.Namespace = site.Namespaces[ns];
			this.PageName = pageName;
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the value corresponding to {{BASEPAGENAME}}.</summary>
		/// <value>The name of the base page.</value>
		public string BasePageName
		{
			get
			{
				if (this.Namespace.AllowsSubpages)
				{
					var subpageLoc = this.PageName.LastIndexOf('/');
					if (subpageLoc >= 0)
					{
						return this.PageName.Substring(0, subpageLoc);
					}
				}

				return this.PageName;
			}
		}

		/// <inheritdoc/>
		public Namespace Namespace { get; set; }

		/// <inheritdoc/>
		public string PageName { get; set; }

		/// <summary>Gets the site to which this title belongs.</summary>
		/// <value>The site.</value>
		public Site Site => this.Namespace.Site;

		/// <summary>Gets a Title object for this Title's corresponding subject page.</summary>
		/// <value>The subject page.</value>
		/// <remarks>If this Title is a subject page, returns itself.</remarks>
		public SimpleTitle SubjectPage => this.Namespace.IsSubjectSpace ? this : new SimpleTitle(this.Namespace.SubjectSpace, this.PageName);

		/// <summary>Gets the value corresponding to {{SUBPAGENAME}}.</summary>
		/// <value>The name of the subpage.</value>
		public string SubpageName
		{
			get
			{
				if (this.Namespace.AllowsSubpages)
				{
					var subpageLoc = this.PageName.LastIndexOf('/') + 1;
					if (subpageLoc > 0)
					{
						return this.PageName.Substring(subpageLoc);
					}
				}

				return this.PageName;
			}
		}

		/// <summary>Gets a Title object for this Title's corresponding subject page.</summary>
		/// <value>The talk page.</value>
		/// <remarks>If this Title is a talk page, the Title returned will be itself. Returns null for pages which have no associated talk page.</remarks>
		public SimpleTitle? TalkPage =>
			this.Namespace.TalkSpace == null ? null :
			this.Namespace.IsTalkSpace ? this :
			new SimpleTitle(this.Namespace.TalkSpace, this.PageName);
		#endregion

		#region Public Methods

		/// <summary>Deconstructs this instance into its constituent parts.</summary>
		/// <param name="ns">The value returned by <see cref="Namespace"/>.</param>
		/// <param name="pageName">The value returned by <see cref="PageName"/>.</param>
		public void Deconstruct(out Namespace ns, out string pageName)
		{
			ns = this.Namespace;
			pageName = this.PageName;
		}

		/// <inheritdoc/>
		public override bool Equals(object? obj) => this.SimpleEquals(obj as ISimpleTitle);

		/// <inheritdoc/>
		public override int GetHashCode() => CompositeHashCode(this.Namespace, this.PageName);

		/// <summary>Checks if the current page name is the same as the specified page name, based on the case-sensitivity for the namespace.</summary>
		/// <param name="pageName">The page name to compare to.</param>
		/// <returns><see langword="true" /> if the two string are considered the same; otherwise <see langword="false" />.</returns>
		/// <remarks>It is assumed that the namespace for the parameter is equal to the current one, or at least that they have the same case-sensitivy.</remarks>
		public bool PageNameEquals(string pageName) => this.Namespace.PageNameEquals(this.PageName, pageName);
		#endregion

		#region Public Override Methods

		/// <inheritdoc/>
		public override string ToString() => this.FullPageName();
		#endregion
	}
}