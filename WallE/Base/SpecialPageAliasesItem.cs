#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	public class SpecialPageAliasesItem
	{
		#region Public Properties
		public IReadOnlyList<string> Aliases { get; set; }

		public string RealName { get; set; }
		#endregion
	}
}
