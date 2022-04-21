namespace RobinHood70.Robby.Design
{
	using RobinHood70.CommonCode;

	// TODO: Review constructors for various title objects.

	/// <summary>Splits a page name into its constituent parts.</summary>
	public class FullTitle : Title, IFullTitle
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="FullTitle"/> class.</summary>
		/// <param name="fullTitle">The <see cref="IFullTitle"/> with the desired information.</param>
		public FullTitle(IFullTitle fullTitle)
			: base(fullTitle.NotNull().Namespace, fullTitle.PageName)
		{
			this.Fragment = fullTitle.Fragment;
			this.Interwiki = fullTitle.Interwiki;
		}

		/// <summary>Initializes a new instance of the <see cref="FullTitle"/> class.</summary>
		/// <param name="title">The <see cref="Title"/> to downcast.</param>
		public FullTitle(Title title)
			: base(title.NotNull())
		{
		}
		#endregion

		#region Public Properties

		/// <summary>Gets or sets the title's fragment (the section or ID to scroll to).</summary>
		/// <value>The fragment.</value>
		public string? Fragment { get; set; }

		/// <summary>Gets or sets the interwiki prefix.</summary>
		/// <value>The interwiki prefix.</value>
		public InterwikiEntry? Interwiki { get; set; }

		/// <summary>Gets a value indicating whether this instance is identical to the local wiki.</summary>
		/// <value><see langword="true"/> if this instance is local wiki; otherwise, <see langword="false"/>.</value>
		public bool IsLocal => this.Interwiki?.LocalWiki != false;
		#endregion

		#region Public Methods

		/// <summary>Deconstructs this instance into its constituent parts.</summary>
		/// <param name="interwiki">The value returned by <see cref="Interwiki"/>.</param>
		/// <param name="ns">The value returned by <see cref="Title.Namespace"/>.</param>
		/// <param name="pageName">The value returned by <see cref="Title.PageName"/>.</param>
		/// <param name="fragment">The value returned by <see cref="Fragment"/>.</param>
		public void Deconstruct(out InterwikiEntry? interwiki, out Namespace ns, out string pageName, out string? fragment)
		{
			interwiki = this.Interwiki;
			ns = this.Namespace;
			pageName = this.PageName;
			fragment = this.Fragment;
		}
		#endregion

		#region Public Override Methods

		/// <summary>Returns a <see cref="string" /> that represents this title.</summary>
		/// <param name="forceLink">if set to <c>true</c>, forces link formatting in namespaces that require it (e.g., Category and File).</param>
		/// <returns>A <see cref="string" /> that represents this title.</returns>
		public override string ToString(bool forceLink)
		{
			var colon = (forceLink && this.Namespace.IsForcedLinkSpace) ? ":" : string.Empty;
			var interwiki = this.Interwiki == null ? string.Empty : this.Interwiki.Prefix + ':';
			var fragment = this.Fragment == null ? string.Empty : '#' + this.Fragment;

			return colon + interwiki + this.FullPageName + fragment;
		}
		#endregion
	}
}