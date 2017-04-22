#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;
	using System.Collections.ObjectModel;

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Project naming convention takes precedence.")]
	public class MoveResult : ReadOnlyCollection<MoveItem>
	{
		#region Constructors
		public MoveResult(IList<MoveItem> list)
			: base(list)
		{
		}
		#endregion

		#region Public Properties
		public string Reason { get; set; }

		public bool RedirectCreated { get; set; }
		#endregion
	}
}
