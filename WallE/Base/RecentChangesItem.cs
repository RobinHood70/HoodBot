#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System;
using System.Collections.Generic;
using RobinHood70.WikiCommon;

#region Public Enumerations
[Flags]
public enum PatrolFlags
{
	None = 0,
	Autopatrolled = 1,
	Patrolled = 1 << 1,
	Unpatrolled = 1 << 2
}

[Flags]
public enum RecentChangesFlags
{
	None = 0,
	Bot = 1,
	Minor = 1 << 1,
	New = 1 << 2,
	Redirect = 1 << 3
}
#endregion

/// <summary>Holds all data for an entry from Special:RecentChanges. Note that a Recent Change is, in essence, a log entry with a few extra properties and is therefore modeled that way. Since log entries can be derived types, themselves, the LogEvent property holds the specific LogEvent derivative, when appropriate, or a base LogEvent object for normal edits.</summary>
public class RecentChangesItem : LogEvent, IApiTitle
{
	#region Constructors
	internal RecentChangesItem(int ns, string title, RecentChangesFlags flags, long id, int newLength, int oldLength, long oldRevisionId, PatrolFlags? patrolFlags, string? patrolToken, string? recentChangeType, long revisionId, IReadOnlyList<string> tags)
	{
		this.Namespace = ns;
		this.Title = title;
		this.Flags = flags;
		this.Id = id;
		this.NewLength = newLength;
		this.OldLength = oldLength;
		this.OldRevisionId = oldRevisionId;
		this.PatrolFlags = patrolFlags;
		this.PatrolToken = patrolToken;
		this.RecentChangeType = recentChangeType;
		this.RevisionId = revisionId;
		this.Tags = tags;
	}
	#endregion

	#region Public Properties
	public RecentChangesFlags Flags { get; }

	public long Id { get; }

	public int Namespace { get; }

	public int NewLength { get; }

	public int OldLength { get; }

	public long OldRevisionId { get; }

	public PatrolFlags? PatrolFlags { get; }

	public string? PatrolToken { get; }

	public string? RecentChangeType { get; }

	public long RevisionId { get; }

	public IReadOnlyList<string> Tags { get; }

	public string Title { get; }
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Title;
	#endregion
}