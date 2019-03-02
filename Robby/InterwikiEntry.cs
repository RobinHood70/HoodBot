namespace RobinHood70.Robby
{
	using RobinHood70.WallE.Base;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Represents information about an interwiki entry.</summary>
	public class InterwikiEntry
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="InterwikiEntry"/> class.</summary>
		/// <param name="site">The site the interwiki entry is from.</param>
		/// <param name="item">The <see cref="InterwikiMapItem"/> item to initialize from.</param>
		protected internal InterwikiEntry(Site site, InterwikiMapItem item)
		{
			ThrowNull(item, nameof(item));
			this.Site = site;
			this.Language = item.Language;
			this.LocalFarm = item.Flags.HasFlag(InterwikiMapFlags.Local);
			this.LocalWiki = item.Flags.HasFlag(InterwikiMapFlags.LocalInterwiki);
			this.Path = item.Url;
			this.Prefix = item.Prefix;
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the language for language links.</summary>
		/// <value>The language.</value>
		public string Language { get; }

		/// <summary>Gets a value indicating whether this interwiki is located on the same wiki farm as the local wiki.</summary>
		/// <value><c>true</c> if this interwiki is located on the local wiki farm; otherwise, <c>false</c>.</value>
		/// <remarks>This flags other wikis that are located on the same wiki farm as the current wiki. While this is usually the case for language links, it can also apply in other cases (e.g., the Wikimedia Commons wiki is on the same farm as the MediaWiki wiki).</remarks>
		public bool LocalFarm { get; }

		/// <summary>Gets a value indicating whether this interwiki link is the current wiki.</summary>
		/// <value><c>true</c> if the interwiki points to the current wiki; otherwise, <c>false</c>.</value>
		public bool LocalWiki { get; private set; }

		/// <summary>Gets the path of the interwiki.</summary>
		/// <value>The path of the interwiki, with <c>$1</c> where the article name should go.</value>
		/// <remarks>This is not represented as a <see cref="System.Uri"/> since it would virtually always have to be converted back to a string for the <c>$1</c> replacement.</remarks>
		public string Path { get; }

		/// <summary>Gets the interwiki prefix.</summary>
		/// <value>The interwiki prefix.</value>
		public string Prefix { get; }

		/// <summary>Gets the site object the interwiki entry is from.</summary>
		/// <value>The site.</value>
		public Site Site { get; }
		#endregion

		#region Public Methods

		/// <summary>Guesses the LocalWiki setting based on the server path.</summary>
		/// <param name="serverPath">Path to the server.</param>
		public void GuessLocalWikiFromServer(string serverPath)
		{
			if (this.Path.Contains(serverPath))
			{
				this.LocalWiki = true;
			}
		}
		#endregion

		#region Public Override Methods

		/// <summary>Returns a <see cref="string" /> that represents this instance.</summary>
		/// <returns>A <see cref="string" /> that represents this instance.</returns>
		public override string ToString() => this.Prefix;
		#endregion
	}
}
