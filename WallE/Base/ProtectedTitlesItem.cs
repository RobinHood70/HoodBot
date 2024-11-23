#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System;
using RobinHood70.WikiCommon;

public class ProtectedTitlesItem(int ns, string title, string? comment, DateTime? expiry, string? level, string? parsedComment, DateTime? timestamp, string? user, long userId) : IApiTitle
{
	#region Public Properties
	public string? Comment { get; } = comment;

	public DateTime? Expiry { get; } = expiry;

	public string? Level { get; } = level;

	public int Namespace { get; } = ns;

	public string? ParsedComment { get; } = parsedComment;

	public DateTime? Timestamp { get; } = timestamp;

	public string Title { get; } = title;

	public string? User { get; } = user;

	public long UserId { get; } = userId;
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Title;
	#endregion
}