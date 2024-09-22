#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	#region Public Enumerations
	[Flags]
	public enum DeletedRevisionsProperties
	{
		None = 0,
		RevId = 1,
		ParentId = 1 << 1,
		User = 1 << 2,
		UserId = 1 << 3,
		Comment = 1 << 4,
		ParsedComment = 1 << 5,
		Minor = 1 << 6,
		Len = 1 << 7,
		Sha1 = 1 << 8,
		Content = 1 << 9,
		Token = 1 << 10,
		Tags = 1 << 11,
		All = RevId | ParentId | User | UserId | Comment | ParsedComment | Minor | Len | Sha1 | Content | Token | Tags
	}
	#endregion

	public class ListDeletedRevisionsInput : ILimitableInput
	{
		#region Public Properties
		public DateTime? End { get; set; }

		public bool ExcludeUser { get; set; }

		public string? From { get; set; }

		public int Limit { get; set; }

		public int MaxItems { get; set; }

		public int? Namespace { get; set; }

		public string? Prefix { get; set; }

		public DeletedRevisionsProperties Properties { get; set; }

		public bool SortAscending { get; set; }

		public DateTime? Start { get; set; }

		public string? Tag { get; set; }

		public string? To { get; set; }

		public bool Unique { get; set; }

		public string? User { get; set; }
		#endregion
	}
}