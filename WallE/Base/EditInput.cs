#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.WallE.Properties;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WallE.ProjectGlobals;
	using static RobinHood70.WikiCommon.Globals;

	public class EditInput
	{
		#region Constructors
		public EditInput(string title, string txt)
		{
			ThrowNullOrWhiteSpace(title, nameof(title));
			ThrowNull(txt, nameof(txt));
			this.Title = title;
			this.Text = txt;
		}

		public EditInput(long pageId, string txt)
		{
			ThrowNull(txt, nameof(txt));
			this.PageId = pageId;
			this.Text = txt;
		}

		public EditInput(string title, string prependText, string appendText)
		{
			ThrowNullOrWhiteSpace(title, nameof(title));
			if (string.IsNullOrEmpty(prependText ?? appendText))
			{
				throw new ArgumentException(CurrentCulture(EveMessages.PrependAppend));
			}

			this.Title = title;
			this.PrependText = prependText;
			this.AppendText = appendText;
		}

		public EditInput(long pageId, string prependText, string appendText)
		{
			if (string.IsNullOrEmpty(prependText ?? appendText))
			{
				throw new ArgumentException(CurrentCulture(EveMessages.PrependAppend));
			}

			this.PageId = pageId;
			this.PrependText = prependText;
			this.AppendText = appendText;
		}

		public EditInput(string title, long undoRevision)
		{
			ThrowNullOrWhiteSpace(title, nameof(title));
			this.Title = title;
			this.UndoRevision = undoRevision;
		}

		public EditInput(long pageId, long undoRevision)
		{
			this.PageId = pageId;
			this.UndoRevision = undoRevision;
		}
		#endregion

		#region Public Properties
		public string AppendText { get; }

		public DateTime? BaseTimestamp { get; set; }

		public bool Bot { get; set; }

		public string ContentFormat { get; set; }

		public string ContentModel { get; set; }

		public string Md5 { get; set; }

		public Tristate Minor { get; set; }

		public long PageId { get; set; }

		public string PrependText { get; }

		public bool Recreate { get; set; }

		public bool Redirect { get; set; }

		public int? Section { get; set; }

		public string SectionTitle { get; set; }

		public DateTime? StartTimestamp { get; set; }

		public string Summary { get; set; }

		public IEnumerable<string> Tags { get; set; }

		public string Text { get; }

		public string Title { get; }

		public string Token { get; set; }

		/// <summary>Gets or sets whether the existence of a page should be considered in making an edit.</summary>
		/// <value>Set to true to only allow creating new pages, never overwriting existing ones; set to false to only allow editing pages, never creating them; leave as null to edit or create pages as necessary.</value>
		/// <remarks>The API will return an error if the requirements are violated, which WallE will in turn throw as a WikiException.</remarks>
		public Tristate RequireNewPage { get; set; }

		public long UndoRevision { get; }

		public long UndoAfterRevision { get; set; }

		public WatchlistOption Watchlist { get; set; }
		#endregion
	}
}
