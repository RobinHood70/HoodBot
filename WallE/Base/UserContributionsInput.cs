#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.WikiCommon;

	#region Public Enumerations
	[Flags]
	public enum UserContribsProperties
	{
		None = 0,
		Ids = 1,
		Title = 1 << 1,
		Timestamp = 1 << 2,
		Comment = 1 << 3,
		ParsedComment = 1 << 4,
		Size = 1 << 5,
		SizeDiff = 1 << 6,
		Flags = 1 << 7,
		Patrolled = 1 << 8,
		Tags = 1 << 9,
		All = Ids | Title | Timestamp | Comment | ParsedComment | Size | SizeDiff | Flags | Patrolled | Tags
	}
	#endregion

	public class UserContributionsInput : ILimitableInput
	{
		#region Constructors
		public UserContributionsInput(string userPrefix) => this.UserPrefix = userPrefix;

		public UserContributionsInput(IEnumerable<string> users) => this.Users = users;
		#endregion

		#region Public Properties
		public DateTime? End { get; set; }

		public Filter FilterMinor { get; set; }

		public Filter FilterNew { get; set; }

		public Filter FilterPatrolled { get; set; }

		public Filter FilterTop { get; set; }

		public int Limit { get; set; }

		public int MaxItems { get; set; }

		public IEnumerable<int> Namespaces { get; set; }

		public UserContribsProperties Properties { get; set; }

		public bool SortAscending { get; set; }

		public DateTime? Start { get; set; }

		public string Tag { get; set; }

		public string UserPrefix { get; }

		public IEnumerable<string> Users { get; }
		#endregion
	}
}
