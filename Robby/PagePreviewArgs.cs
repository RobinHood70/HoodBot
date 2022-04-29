namespace RobinHood70.Robby
{
	/// <summary>Event data for any events where page text is changing.</summary>
	public class PagePreviewArgs
	{
		#region Constructors

		internal PagePreviewArgs(PageTextChangeArgs args)
		{
			this.MethodName = args.MethodName;
			this.Page = args.Page;
			this.EditSummary = args.EditSummary;
			this.Minor = args.Minor;
			this.BotEdit = args.BotEdit;
			this.RecreateIfJustDeleted = args.RecreateIfJustDeleted;
		}
		#endregion

		#region Public Properties

		/// <summary>Gets a value indicating whether the edit should be marked as a bot edit.</summary>
		public bool BotEdit { get; }

		/// <summary>Gets the edit summary.</summary>
		public string EditSummary { get; }

		/// <summary>Gets the sender of the warning.</summary>
		/// <value>The sender of the warning.</value>
		public string MethodName { get; }

		/// <summary>Gets a value indicating whether the edit should be marked as minor.</summary>
		public bool Minor { get; }

		/// <summary>Gets the page whose text is being changed.</summary>
		/// <value>The parameters.</value>
		public Page Page { get; }

		/// <summary>Gets a value indicating whether the page should be recreated if it was deleted since the edit began.</summary>
		public bool RecreateIfJustDeleted { get; }

		/// <summary>Gets or sets the edit token.</summary>
		public string? Token { get; set; }
		#endregion
	}
}