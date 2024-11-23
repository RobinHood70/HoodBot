#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using RobinHood70.CommonCode;

#region Public Enumerations
public enum EditTextType
{
	Normal,
	Prepend,
	Append
}
#endregion

public class EditInput
{
	#region Constructors
	public EditInput(string title, [Localizable(false)] string text)
		: this(title, text, EditTextType.Normal)
	{
	}

	public EditInput(string title, [Localizable(false)] string text, EditTextType textType)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(title);
		ArgumentNullException.ThrowIfNull(text);
		this.Title = title;
		switch (textType)
		{
			case EditTextType.Normal:
				this.Text = text;
				break;
			case EditTextType.Prepend:
				this.PrependText = text;
				break;
			case EditTextType.Append:
				this.AppendText = text;
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(textType));
		}
	}

	public EditInput(long pageId, [Localizable(false)] string text)
		: this(pageId, text, EditTextType.Normal)
	{
	}

	public EditInput(long pageId, [Localizable(false)] string text, EditTextType textType)
	{
		ArgumentNullException.ThrowIfNull(text);
		this.PageId = pageId;
		switch (textType)
		{
			case EditTextType.Normal:
				this.Text = text;
				break;
			case EditTextType.Prepend:
				this.PrependText = text;
				break;
			case EditTextType.Append:
				this.AppendText = text;
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(textType));
		}
	}

	public EditInput(string title, string prependText, string appendText)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(title);
		ArgumentException.ThrowIfNullOrWhiteSpace(prependText);
		ArgumentException.ThrowIfNullOrWhiteSpace(appendText);
		this.Title = title;
		this.PrependText = prependText;
		this.AppendText = appendText;
	}

	public EditInput(long pageId, string prependText, string appendText)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(prependText);
		ArgumentException.ThrowIfNullOrWhiteSpace(appendText);
		this.PageId = pageId;
		this.PrependText = prependText;
		this.AppendText = appendText;
	}

	public EditInput(string title, long undoRevision)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(title);
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
	public string? AppendText { get; }

	public DateTime? BaseTimestamp { get; set; }

	public bool Bot { get; set; }

	public IDictionary<string, string> CaptchaSolution { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

	public string? ContentFormat { get; set; }

	public string? ContentModel { get; set; }

	public string? Md5 { get; set; }

	public Tristate Minor { get; set; }

	public long PageId { get; set; }

	public string? PrependText { get; }

	public bool Recreate { get; set; }

	public bool Redirect { get; set; }

	public int? Section { get; set; }

	public string? SectionTitle { get; set; }

	public DateTime? StartTimestamp { get; set; }

	public string? Summary { get; set; }

	public IEnumerable<string>? Tags { get; set; }

	public string? Text { get; }

	public string? Title { get; }

	public string? Token { get; set; }

	/// <summary>Gets or sets whether the existence of a page should be considered in making an edit.</summary>
	/// <value>Set to true to only allow creating new pages, never overwriting existing ones; set to false to only allow editing pages, never creating them; leave as null to edit or create pages as necessary.</value>
	/// <remarks>The API will return an error if the requirements are violated, which WallE will in turn throw as a WikiException.</remarks>
	public Tristate RequireNewPage { get; set; }

	public long UndoRevision { get; }

	public long UndoAfterRevision { get; set; }

	public WatchlistOption Watchlist { get; set; }
	#endregion
}