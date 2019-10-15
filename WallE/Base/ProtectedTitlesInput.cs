#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	#region Public Enumerations
	[Flags]
	public enum ProtectedTitlesProperties
	{
		None = 0,
		Timestamp = 1,
		User = 1 << 1,
		UserId = 1 << 2,
		Comment = 1 << 3,
		ParsedComment = 1 << 4,
		Expiry = 1 << 5,
		Level = 1 << 6,
		All = Timestamp | User | UserId | Comment | ParsedComment | Expiry | Level
	}
	#endregion

	public class ProtectedTitlesInput : ILimitableInput, IGeneratorInput
	{
		#region Public Properties
		public DateTime? End { get; set; }

		public IEnumerable<string>? Levels { get; set; }

		public int Limit { get; set; }

		public int MaxItems { get; set; }

		public IEnumerable<int>? Namespaces { get; set; }

		public ProtectedTitlesProperties Properties { get; set; }

		public bool SortAscending { get; set; }

		public DateTime? Start { get; set; }
		#endregion
	}
}
