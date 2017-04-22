#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;
	using System.Collections.ObjectModel;

	// IMPNOTE: Only the base class of items is currently parsed due to the relative complexity and relative uselessness of parsing the specific info for each type. The query API should have most or all of the same info anyway, if it's really needed.
	// IMPNOTE: Only rendered errors and warnings are returned, not the full structure. Again, it seemed like overkill to return everything. This means English-only error messages unless MW takes it further in future versions.
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Project naming convention takes precedence.")]
	public class RevisionDeleteResult : ReadOnlyCollection<RevisionDeleteItem>
	{
		#region Constructors
		public RevisionDeleteResult(IList<RevisionDeleteItem> list)
			: base(list)
		{
		}
		#endregion

		#region Public Properties
		public IReadOnlyList<string> Errors { get; set; }

		public string Status { get; set; }

		public string Target { get; set; }

		public IReadOnlyList<string> Warnings { get; set; }
		#endregion
	}
}
