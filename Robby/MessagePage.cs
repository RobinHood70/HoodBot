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
	public sealed class MessagePage : Page
	{
		// TODO: MessagePage has different data loaded depending whether it's a faked page or a genuine message. Is this a good idea? Loading all data would require calls to both Load and AllMessages, which could be undesirable in the PageCreator. Might be a better idea to split this into Message and MessagePage objects depending on behaviour desired, with Message being custom and light-weight.
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="MessagePage"/> class.</summary>
		/// <param name="title">The <see cref="Title"/> to copy values from.</param>
		/// <param name="options">The load options used for this page. Can be used to detect if default-valued information is legitimate or was never loaded.</param>
		/// <param name="apiItem">The API item to extract information from.</param>
		internal MessagePage(ITitle title, PageLoadOptions options, IApiTitle? apiItem)
			: base(title, options, apiItem)
		{
			if (title.Title.Namespace.Id != MediaWikiNamespaces.MediaWiki)
			{
				throw new ArgumentException(
					Globals.CurrentCulture(Resources.NamespaceMustBe, title.Title.Site[MediaWikiNamespaces.MediaWiki].Name),
					nameof(title));
			}

			if (apiItem is PageItem && !this.IsMissing)
			{
				AllMessagesInput input = new() { Messages = new[] { title.Title.PageName } };
				var result = this.Site.AbstractionLayer.AllMessages(input);
				switch (result.Count)
				{
					case 0:
						this.IsMissing = true;
						break;
					case 1:
						var item = result[0];
						this.IsInvalid = false;
						this.IsMissing = item.Flags.HasAnyFlag(MessageFlags.Missing);
						this.Customized = item.Flags.HasAnyFlag(MessageFlags.Customized);
						this.DefaultMissing = item.Flags.HasAnyFlag(MessageFlags.DefaultMissing);
						this.DefaultMessage = item.Default;
						this.NormalizedName = item.NormalizedName;
						this.Text = item.Content ?? item.Default;
						break;
					default:
						// TODO: Should probably throw an error here.
						this.IsInvalid = true;
						break;
				}
			}
		}

		/// <summary>Initializes a new instance of the <see cref="MessagePage"/> class.</summary>
		/// <param name="title">The <see cref="Title"/> to copy values from.</param>
		/// <param name="item">The AllMessagesItem to populate this instance from.</param>
		internal MessagePage(ITitle title, AllMessagesItem item)
			: base(title, PageLoadOptions.None, null)
		{
			ArgumentNullException.ThrowIfNull(item);
			this.IsInvalid = false;
			this.IsMissing = item.Flags.HasAnyFlag(MessageFlags.Missing);
			this.Customized = item.Flags.HasAnyFlag(MessageFlags.Customized);
			this.DefaultMissing = item.Flags.HasAnyFlag(MessageFlags.DefaultMissing);
			this.DefaultMessage = item.Default;
			this.NormalizedName = item.NormalizedName;
			this.Text = item.Content ?? item.Default ?? string.Empty;
		}
		#endregion

		#region Public Properties

		/// <summary>Gets a value indicating whether this <see cref="MessagePage"/> has been customized.</summary>
		/// <value><see langref="true" /> if customized; otherwise, <see langref="false" />.</value>
		public bool Customized { get; }

		/// <summary>Gets the default message.</summary>
		/// <value>The default message.</value>
		/// <remarks>If the message has been loaded via any of the <see cref="Site" /> GetMessage-related methods, this will contain the default version of the message, even if it has since been customized.</remarks>
		public string? DefaultMessage { get; }

		/// <summary>Gets a value indicating whether the default value was missing.</summary>
		/// <value><see langref="true" /> if the default value is missing; otherwise, <see langref="false" />.</value>
		public bool DefaultMissing { get; }

		/// <summary>Gets the normalized name of the message.</summary>
		/// <value>The normalized name.</value>
		/// <remarks>For messages, spaces will be replaced by underscores, and the first letter will be converted to lower-case.</remarks>
		public string? NormalizedName { get; }
		#endregion
	}
}