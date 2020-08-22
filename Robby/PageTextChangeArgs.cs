namespace RobinHood70.Robby
{
	/// <summary>Event data for any events where page text is changing.</summary>
	public class PageTextChangeArgs
	{
		#region Fields
		private bool cancelChange;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="PageTextChangeArgs"/> class.</summary>
		/// <param name="page">The page being changed.</param>
		/// <param name="methodName">The method name.</param>
		/// <param name="editSummary">The edit summary.</param>
		/// <param name="isMinor">Whether the edit should be marked as minor.</param>
		/// <param name="isBotEdit">Whether the edit should be marked as a bot edit.</param>
		/// <param name="recreateIfJustDeleted">Whether the page should be recreated if it was deleted since the edit began.</param>
		public PageTextChangeArgs(Page page, string methodName, string editSummary, bool isMinor, bool isBotEdit, bool recreateIfJustDeleted)
		{
			this.MethodName = methodName;
			this.Page = page;
			this.EditSummary = editSummary;
			this.Minor = isMinor;
			this.BotEdit = isBotEdit;
			this.RecreateIfJustDeleted = recreateIfJustDeleted;
		}
		#endregion

		#region Public Properties

		/// <summary>Gets or sets a value indicating whether the edit should be marked as a bot edit.</summary>
		public bool BotEdit { get; set; }

		/// <summary>Gets or sets a value indicating whether the desired edit should be cancelled.</summary>
		/// <value><see langword="true"/> if the change should be cancelled; otherwise, <see langword="false"/>.</value>
		/// <remarks>Once this property is set to <see langword="true"/>, it cannot be changed.</remarks>
		public bool CancelChange
		{
			get => this.cancelChange;
			set => this.cancelChange |= value;
		}

		/// <summary>Gets or sets the edit summary.</summary>
		public string EditSummary { get; set; }

		/// <summary>Gets the sender of the warning.</summary>
		/// <value>The sender of the warning.</value>
		public string MethodName { get; }

		/// <summary>Gets or sets a value indicating whether the edit should be marked as minor.</summary>
		public bool Minor { get; set; }

		/// <summary>Gets the page whose text is being changed.</summary>
		/// <value>The parameters.</value>
		public Page Page { get; }

		/// <summary>Gets or sets a value indicating whether the page should be recreated if it was deleted since the edit began.</summary>
		public bool RecreateIfJustDeleted { get; set; }
		#endregion
	}
}
