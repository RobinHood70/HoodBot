#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class SectionsItem
	{
		#region Constructors
		internal SectionsItem(int tocLevel, int level, string anchor, string line, string number, string index, int? byteOffset, string? fromTitle)
		{
			this.TocLevel = tocLevel;
			this.Level = level;
			this.Anchor = anchor;
			this.Line = line;
			this.Number = number;
			this.Index = index;
			this.ByteOffset = byteOffset;
			this.FromTitle = fromTitle;
		}
		#endregion

		#region Public Properties
		public string Anchor { get; }

		public int? ByteOffset { get; }

		public string? FromTitle { get; }

		public string Index { get; }

		public int Level { get; }

		public string Line { get; }

		public string Number { get; }

		public int TocLevel { get; }
		#endregion

		#region Public Override Methods
		public override string ToString()
		{
			var equals = new string('=', this.Level);
			return equals + this.Line + equals;
		}
		#endregion
	}
}