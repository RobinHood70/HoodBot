#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System;

[Flags]
public enum SlotFlags
{
	None = 0,
	BadContentFormat = 1,
	Missing = 1 << 1,
	NoSuchSection = 1 << 2,
	Sha1Hidden = 1 << 3,
	TextHidden = 1 << 4,
	TextMissing = 1 << 5,
}

public class SlotItem
{
	internal SlotItem(string? content, string? contentFormat, string? contentModel, SlotFlags flags, string? sha1, long size)
	{
		this.Content = content;
		this.ContentFormat = contentFormat;
		this.ContentModel = contentModel;
		this.Flags = flags;
		this.Sha1 = sha1;
		this.Size = size;
	}

	public string? Content { get; }

	public string? ContentFormat { get; }

	public string? ContentModel { get; }

	public SlotFlags Flags { get; }

	public string? Sha1 { get; }

	public long Size { get; }
}