namespace RobinHood70.Robby
{
	using WallE.Base;
	using WikiCommon;
	using static WikiCommon.Globals;

	/// <summary>Stores a MediaWiki page along with associated data.</summary>
	/// <seealso cref="RobinHood70.Robby.Page" />
	public class Message : Page
	{
		// TODO: Message has different data loaded depending whether it's a faked page or a genuine message. Is this a good idea? Loading all data would require calls to both Load and AllMessages, which could be a undesirable in the PageCreator. Might be a better idea to split this into Message and MessagePage objects depending on behaviour desired, with Message being custom and light-weight.
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="Message"/> class.</summary>
		/// <param name="site">The site the page is from.</param>
		/// <param name="pageName">The page name (<em>without</em> the leading namespace).</param>
		protected internal Message(Site site, string pageName)
			: base(site, MediaWikiNamespaces.MediaWiki, pageName)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="Message"/> class.</summary>
		/// <param name="site">The site the page is from.</param>
		/// <param name="item">The AllMessagesItem to populate this instance from.</param>
		protected internal Message(Site site, AllMessagesItem item)
			: base(site, MediaWikiNamespaces.MediaWiki, item?.Name) => this.PopulateFrom(item);
		#endregion

		#region Public Properties

		/// <summary>Gets a value indicating whether this <see cref="Message"/> has been customized.</summary>
		/// <value><see langref="true" /> if customized; otherwise, <see langref="false" />.</value>
		public bool Customized { get; private set; }

		/// <summary>Gets the default message.</summary>
		/// <value>The default message.</value>
		/// <remarks>If the message has been loaded via any of the <see cref="T:Site" /> GetMessage-related methods, this will contain the default version of the message, even if it has since been customized.</remarks>
		public string DefaultMessage { get; private set; }

		/// <summary>Gets a value indicating whether the default value was missing.</summary>
		/// <value><see langref="true" /> if the default value is missing; otherwise, <see langref="false" />.</value>
		public bool DefaultMissing { get; private set; }

		/// <summary>Gets the normalized name of the message.</summary>
		/// <value>The normalized name.</value>
		/// <remarks>For messages, spaces will be replaced by underscores, and the first letter will be converted to lower-case.</remarks>
		public string NormalizedName { get; private set; }
		#endregion

		#region Protected Internal Methods

		/// <summary>Populates this message instance from the specified item.</summary>
		/// <param name="item">The item to populate from.</param>
		protected internal void PopulateFrom(AllMessagesItem item)
		{
			ThrowNull(item, nameof(item));
			this.Invalid = false;
			this.Customized = item.Flags.HasFlag(MessageFlags.Customized);
			this.DefaultMissing = item.Flags.HasFlag(MessageFlags.DefaultMissing);
			this.Missing = item.Flags.HasFlag(MessageFlags.Missing);
			this.DefaultMessage = item.Default;
			this.NormalizedName = item.NormalizedName;
			this.Text = item.Content ?? item.Default;
		}
		#endregion

		#region Protected Override Methods

		/// <summary>Populates Message-specific properties from default values if a customized page was not found.</summary>
		/// <param name="pageItem">The page item.</param>
		protected override void PopulateCustomResults(PageItem pageItem)
		{
			ThrowNull(pageItem, nameof(pageItem));
			if (pageItem.Flags.HasFlag(PageFlags.Missing))
			{
				var input = new AllMessagesInput() { Messages = new[] { this.PageName } };
				var result = this.Site.AbstractionLayer.AllMessages(input);
				if (result.Count == 1)
				{
					this.PopulateFrom(result[0]);
				}
				else
				{
					this.Invalid = true;
				}
			}
		}
		#endregion
	}
}