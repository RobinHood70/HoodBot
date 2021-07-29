namespace RobinHood70.Robby
{
	using System;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Properties;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;

	/// <summary>Stores a MediaWiki page along with associated data.</summary>
	/// <seealso cref="Page" />
	public class MessagePage : Page
	{
		// TODO: MessagePage has different data loaded depending whether it's a faked page or a genuine message. Is this a good idea? Loading all data would require calls to both Load and AllMessages, which could be undesirable in the PageCreator. Might be a better idea to split this into Message and MessagePage objects depending on behaviour desired, with Message being custom and light-weight.
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="MessagePage" /> class.</summary>
		/// <param name="site">The site the Message is from.</param>
		/// <param name="pageName">The page name.</param>
		public MessagePage(Site site, string pageName)
			: base((site.NotNull(nameof(site)))[MediaWikiNamespaces.MediaWiki], pageName)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="MessagePage" /> class.</summary>
		/// <param name="ns">The namespace (must be Message).</param>
		/// <param name="pageName">The page name (<em>without</em> the leading namespace).</param>
		public MessagePage(Namespace ns, string pageName)
			: base(ns, pageName)
		{
			if (ns.Id != MediaWikiNamespaces.MediaWiki)
			{
				throw new ArgumentException(Globals.CurrentCulture(Resources.NamespaceMustBe, ns.Site[MediaWikiNamespaces.MediaWiki].Name), nameof(ns));
			}
		}

		/// <summary>Initializes a new instance of the <see cref="MessagePage" /> class.</summary>
		/// <param name="title">The <see cref="ISimpleTitle"/> to copy values from.</param>
		public MessagePage(ISimpleTitle title)
			: base(title)
		{
			if (title.Namespace.Id != MediaWikiNamespaces.MediaWiki)
			{
				throw new ArgumentException(Globals.CurrentCulture(Resources.NamespaceMustBe, this.Site[MediaWikiNamespaces.MediaWiki].Name), nameof(title));
			}
		}

		/// <summary>Initializes a new instance of the <see cref="MessagePage"/> class.</summary>
		/// <param name="ns">The namespace of the page (must be MediaWiki).</param>
		/// <param name="item">The AllMessagesItem to populate this instance from.</param>
		protected internal MessagePage(Namespace ns, AllMessagesItem item)
			: base(ns, (item.NotNull(nameof(item))).Name) => this.PopulateFrom(item);
		#endregion

		#region Public Properties

		/// <summary>Gets a value indicating whether this <see cref="MessagePage"/> has been customized.</summary>
		/// <value><see langref="true" /> if customized; otherwise, <see langref="false" />.</value>
		public bool Customized { get; private set; }

		/// <summary>Gets the default message.</summary>
		/// <value>The default message.</value>
		/// <remarks>If the message has been loaded via any of the <see cref="Site" /> GetMessage-related methods, this will contain the default version of the message, even if it has since been customized.</remarks>
		public string? DefaultMessage { get; private set; }

		/// <summary>Gets a value indicating whether the default value was missing.</summary>
		/// <value><see langref="true" /> if the default value is missing; otherwise, <see langref="false" />.</value>
		public bool DefaultMissing { get; private set; }

		/// <summary>Gets the normalized name of the message.</summary>
		/// <value>The normalized name.</value>
		/// <remarks>For messages, spaces will be replaced by underscores, and the first letter will be converted to lower-case.</remarks>
		public string? NormalizedName { get; private set; }
		#endregion

		#region Protected Internal Methods

		/// <summary>Populates this message instance from the specified item.</summary>
		/// <param name="item">The item to populate from.</param>
		protected internal void PopulateFrom(AllMessagesItem item)
		{
			this.PopulateFlags(false, (item.NotNull(nameof(item)).Flags & MessageFlags.Missing) != 0);
			this.Customized = (item.Flags & MessageFlags.Customized) != 0;
			this.DefaultMissing = (item.Flags & MessageFlags.DefaultMissing) != 0;
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
			if ((pageItem.NotNull(nameof(pageItem)).Flags & PageFlags.Missing) != 0)
			{
				var input = new AllMessagesInput() { Messages = new[] { this.PageName } };
				var result = this.Site.AbstractionLayer.AllMessages(input);
				if (result.Count == 1)
				{
					this.PopulateFrom(result[0]);
				}
				else
				{
					this.PopulateFlags(true, true);
				}
			}
		}
		#endregion
	}
}