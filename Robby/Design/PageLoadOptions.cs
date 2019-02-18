namespace RobinHood70.Robby.Design
{
	using System;
	using static RobinHood70.WikiCommon.Globals;

	#region Public Enumerations

	/// <summary>Controls which modules are loaded during a page load operation.</summary>
	[Flags]
	public enum PageModules
	{
		/// <summary>Do not load anything beyond basic title information, including whether the page is invalid or missing.</summary>
		None = 0,

		/// <summary>Load the categories the page has been placed into.</summary>
		Categories = 1,

		/// <summary>Load basic information about the page including last revision ID, current timestamp, and whether the page is new or a redirect.</summary>
		Info = 1 << 1,

		/// <summary>Load the list of pages linked to from the current page.</summary>
		Links = 1 << 2,

		/// <summary>Load the page properties.</summary>
		Properties = 1 << 3,

		/// <summary>Load page revisions. This is required in order to load the current page text.</summary>
		Revisions = 1 << 4,

		/// <summary>Load the list of templates transcluded by the current page.</summary>
		/// <remarks>Any transclusion counts as a template for the purposes of this list, not just those in template space.</remarks>
		Templates = 1 << 5,

		/// <summary>On File-space pages, load file revision info, which includes information such as dimensions, MIME type, and pixel depth, along with the user and timestamp for the revision.</summary>
		FileInfo = 1 << 6,

		/// <summary>Load the list of categories in which this page is categorized.</summary>
		CategoryInfo = 1 << 7,

		/// <summary>Load the list of backlinks to the page. When part of a pageset, this allows getting backlinks for multiple pages at once.</summary>
		LinksHere = 1 << 8,

		/// <summary>Load the list of transclusions of this page. When part of a pageset, this allows getting transclusions for multiple pages at once.</summary>
		TranscludedIn = 1 << 9,

		/// <summary>Load custom page information. Use this in conjunction with a custom PageCreator to control when your custom information is retrieved.</summary>
		Custom = 1 << 15,

		/// <summary>Loads default page information, including flags, current timestamp, and current page text.</summary>
		Default = Info | Revisions,

		/// <summary>Load everything Robby is capable of handling.</summary>
		All = Categories | CategoryInfo | FileInfo | Info | Links | LinksHere | Properties | Revisions | Templates | TranscludedIn | Custom
	}
	#endregion

	/// <summary>Provides a central method to specify which modules are loaded during a page load operation, and provides options for some of those modules.</summary>
	public class PageLoadOptions
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="PageLoadOptions"/> class with default options.</summary>
		public PageLoadOptions()
			: this(PageModules.Default)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="PageLoadOptions"/> class with a custom set of modules.</summary>
		/// <param name="modules">The modules to load.</param>
		public PageLoadOptions(PageModules modules) => this.Modules = modules;

		/// <summary>Initializes a new instance of the <see cref="PageLoadOptions"/> class. This is a <em>partial</em> copy constructor which only copies the non-module-related options.</summary>
		/// <param name="copy">The load options to copy from.</param>
		/// <param name="newModules">The new set of modules.</param>
		public PageLoadOptions(PageLoadOptions copy, PageModules newModules)
			: this(newModules)
		{
			ThrowNull(copy, nameof(copy));
			this.ConvertTitles = copy.ConvertTitles;
			this.FollowRedirects = copy.FollowRedirects;
		}
		#endregion

		#region Public Static Properties

		/// <summary>Gets a new PageLoadOptions that loads nothing but title information.</summary>
		/// <value>A new PageLoadOptions that loads nothing but title information.</value>
		public static PageLoadOptions None => new PageLoadOptions(PageModules.None);

		/// <summary>Gets a new PageLoadOptions that loads default information.</summary>
		/// <value>A new PageLoadOptions that loads default information.</value>
		public static PageLoadOptions Default => new PageLoadOptions(PageModules.Default);

		/// <summary>Gets a new PageLoadOptions that loads all available information.</summary>
		/// <value>A new PageLoadOptions that loads all available information.</value>
		public static PageLoadOptions All => new PageLoadOptions(PageModules.Default);
		#endregion

		#region Public Properties

		/// <summary>Gets or sets a value indicating whether to convert titles to other language variants when necessary.</summary>
		/// <value><c>true</c> to convert titles; otherwise, <c>false</c>.</value>
		public bool ConvertTitles { get; set; }

		/// <summary>Gets or sets the number of File revisions to retrieve.</summary>
		/// <value>The number of File revisions to be retrieved.</value>
		public int FileRevisionCount { get; set; }

		/// <summary>Gets or sets a value indicating whether to follow redirects.</summary>
		/// <value><c>true</c> if redirects should be followed; otherwise, <c>false</c>.</value>
		public bool FollowRedirects { get; set; }

		/// <summary>Gets the modules to load.</summary>
		/// <value>The modules to load.</value>
		public PageModules Modules { get; }

		/// <summary>Gets or sets the number of revisions to retrieve.</summary>
		/// <value>The number of revisions to retrieve.</value>
		public int RevisionCount { get; set; }

		/// <summary>Gets or sets the date to retrieve revisions from.</summary>
		/// <value>The date to retrieve revisions from.</value>
		public DateTime? RevisionFrom { get; set; }

		/// <summary>Gets or sets the ID to retrieve revisions from.</summary>
		/// <value>The ID to retrieve revisions from.</value>
		public long RevisionFromId { get; set; }

		/// <summary>Gets or sets a value indicating whether to retrieve revisions in ascending order if only a From or To option is provided.</summary>
		/// <value><c>true</c> if revisions should be loaded <em>from</em> the specified date forward or up <em>to</em> the specified date; otherwise, <c>false</c>.</value>
		public bool RevisionNewer { get; set; }

		/// <summary>Gets or sets the date to retrieve revisions to.</summary>
		/// <value>The date to retrieve revisions to.</value>
		public DateTime? RevisionTo { get; set; }

		/// <summary>Gets or sets the ID to retrieve revisions to.</summary>
		/// <value>The ID to retrieve revisions to.</value>
		public long RevisionToId { get; set; }
		#endregion
	}
}