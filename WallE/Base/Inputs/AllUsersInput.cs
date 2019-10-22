#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	#region public Enumerations
	[Flags]
	public enum AllUsersProperties
	{
		None = 0,
		BlockInfo = 1,
		Groups = 1 << 1,
		ImplicitGroups = 1 << 2,
		Rights = 1 << 3,
		EditCount = 1 << 4,
		Registration = 1 << 5,
		All = BlockInfo | Groups | ImplicitGroups | Rights | EditCount | Registration
	}
	#endregion

	public class AllUsersInput : ILimitableInput
	{
		#region Public Properties
		public bool ActiveUsersOnly { get; set; }

		public bool ExcludeGroups { get; set; }

		public string? From { get; set; }

		public IEnumerable<string>? Groups { get; set; }

		public int Limit { get; set; }

		public int MaxItems { get; set; }

		public string? Prefix { get; set; }

		public AllUsersProperties Properties { get; set; }

		public IEnumerable<string>? Rights { get; set; }

		public bool SortDescending { get; set; }

		public string? To { get; set; }

		public bool WithEditsOnly { get; set; }
		#endregion
	}
}
