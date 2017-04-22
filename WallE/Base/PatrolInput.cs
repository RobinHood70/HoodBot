#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	public class PatrolInput
	{
		#region Constructors
		public PatrolInput(long rcid) => this.RecentChangesId = rcid;

		private PatrolInput()
		{
		}
		#endregion

		#region Public Properties
		public long RecentChangesId { get; }

		public long RevisionId { get; private set; }

		public IEnumerable<string> Tags { get; set; }

		public string Token { get; set; }
		#endregion

		#region Public Static Methods
		public static PatrolInput FromRevisionId(long revid) => new PatrolInput() { RevisionId = revid };
		#endregion
	}
}