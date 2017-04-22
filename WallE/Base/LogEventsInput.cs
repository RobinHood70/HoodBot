#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using static Globals;

	#region Public Enumerations
	[Flags]
	public enum LogEventsProperties
	{
		None = 0,
		Ids = 1,
		Title = 1 << 1,
		Type = 1 << 2,
		User = 1 << 3,
		UserId = 1 << 4,
		Timestamp = 1 << 5,
		Comment = 1 << 6,
		ParsedComment = 1 << 7,
		Details = 1 << 8,
		Tags = 1 << 9,
		All = Ids | Title | Type | User | UserId | Timestamp | Comment | ParsedComment | Details | Tags
	}
	#endregion

	public class LogEventsInput : ILimitableInput
	{
		#region Constructors
		public LogEventsInput()
		{
		}

		public LogEventsInput(int ns) => this.Namespace = ns;

		public LogEventsInput(string title)
		{
			ThrowNullOrWhiteSpace(title, nameof(title));
			this.Title = title;
		}
		#endregion

		#region Public Properties
		public string Action { get; set; }

		public DateTime? End { get; set; }

		public int Limit { get; set; }

		public int MaxItems { get; set; }

		public int? Namespace { get; }

		public string Prefix { get; private set; }

		public LogEventsProperties Properties { get; set; }

		public bool SortDescending { get; set; }

		public DateTime? Start { get; set; }

		public string Tag { get; set; }

		public string Title { get; }

		public string Type { get; set; }

		public string User { get; set; }
		#endregion

		#region Public Static Methods
		public static LogEventsInput FromPrefix(string prefix)
		{
			ThrowNullOrWhiteSpace(prefix, nameof(prefix));
			return new LogEventsInput() { Prefix = prefix };
		}
		#endregion
	}
}
