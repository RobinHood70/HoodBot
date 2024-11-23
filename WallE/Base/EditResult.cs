#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System;
using System.Collections.Generic;

[Flags]
public enum EditFlags
{
	None = 0,
	New = 1,
	NoChange = 1 << 1
}

public class EditResult
{
	#region Constructors
	internal EditResult(string result, long pageId, string title, EditFlags flags, string? contentModel, long oldRevisionId, long newRevisionId, DateTime? newTimestamp, IReadOnlyDictionary<string, string> captchaData)
	{
		this.Result = result;
		this.PageId = pageId;
		this.Title = title;
		this.Flags = flags;
		this.ContentModel = contentModel;
		this.OldRevisionId = oldRevisionId;
		this.NewRevisionId = newRevisionId;
		this.NewTimestamp = newTimestamp;
		this.CaptchaData = captchaData;
	}
	#endregion

	#region Public Properties
	public string? ContentModel { get; }

	public IReadOnlyDictionary<string, string> CaptchaData { get; }

	public EditFlags Flags { get; }

	public long NewRevisionId { get; }

	public DateTime? NewTimestamp { get; }

	public long OldRevisionId { get; }

	public long PageId { get; }

	public string Result { get; }

	public string Title { get; }
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Title;
	#endregion
}