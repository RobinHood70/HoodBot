#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	#region Public Enumerations
	[Flags]
	public enum RevisionsProperties
	{
		None = 0,
		Ids = 1,
		Flags = 1 << 1,
		Timestamp = 1 << 2,
		User = 1 << 3,
		UserId = 1 << 4,
		Size = 1 << 5,
		Sha1 = 1 << 6,
		ContentModel = 1 << 7,
		Comment = 1 << 8,
		ParsedComment = 1 << 9,
		Content = 1 << 10,
		Tags = 1 << 11,
		SlotSize = 1 << 12,
		SlotSha1 = 1 << 13,
		Roles = 1 << 14,
		All = Ids | Flags | Timestamp | User | UserId | Size | Sha1 | ContentModel | Comment | ParsedComment | Content | Tags | SlotSize | SlotSha1 | Roles,
		NoContent = All & ~Content
	}
	#endregion

	public interface IRevisionsInput : ILimitableInput
	{
		#region Public Properties

		/// <summary>Gets or sets the revision ID to compare a diff to.</summary>
		/// <value>The ID to compare to.</value>
		/// <remarks>This can be an integer or any of the <see cref="MediaWikiGlobal" /> DiffTo constants.</remarks>
		int? DiffTo { get; set; }

		string? DiffToText { get; set; }

		bool DiffToTextPreSaveTransform { get; set; }

		DateTime? End { get; }

		bool ExcludeUser { get; set; }

		bool ExpandTemplates { get; set; }

		bool GenerateXml { get; set; }

		bool Parse { get; set; }

		RevisionsProperties Properties { get; set; }

		int Section { get; set; }

		IEnumerable<SlotInput>? Slots { get; set; }

		bool SortAscending { get; set; }

		DateTime? Start { get; }

		string? User { get; set; }
		#endregion
	}
}