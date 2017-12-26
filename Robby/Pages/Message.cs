namespace RobinHood70.Robby.Pages
{
	using WallE.Base;

	public class Message : Page
	{
		// TODO: Message has different data loaded depending whether it's a faked page or a genuine message. Is this a good idea? Loading all data would require calls to both Load and AllMessages, which could be a undesirable in the PageBuilder. Might be a better idea to split this into Message and MessagePage objects depending on behaviour desired, with Message being custom and light-weight.
		#region Constructors
		public Message(Site site, string fullPageName)
			: base(site, fullPageName)
		{
		}

		public Message(Site site, int ns, string pageName)
			: base(site, ns, pageName)
		{
		}
		#endregion

		#region Public Properties
		public bool Customized { get; private set; }

		public string DefaultMessage { get; private set; }

		public bool DefaultMissing { get; private set; }

		public string NormalizedName { get; private set; }
		#endregion

		#region Public Methods
		public void Populate(AllMessagesItem item)
		{
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
		protected override void PopulateCustomResults(PageItem pageItem)
		{
			if (pageItem.Flags.HasFlag(PageFlags.Missing))
			{
				this.LoadDefault();
			}
		}
		#endregion

		#region Private Methods
		private void LoadDefault()
		{
			var input = new AllMessagesInput() { Messages = new[] { this.PageName } };
			var result = this.Site.AbstractionLayer.AllMessages(input);
			if (result.Count == 1)
			{
				this.Populate(result[0]);
			}
			else
			{
				this.Invalid = true;
			}
		}
		#endregion
	}
}