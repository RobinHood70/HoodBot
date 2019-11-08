#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;
	using System.Globalization;

	public class RevisionDeleteItem
	{
		#region Constructors
		internal RevisionDeleteItem(string status, long id, IReadOnlyList<string> errors, IReadOnlyList<string> warnings, RevisionItem revision)
		{
			this.Status = status;
			this.Id = id;
			this.Errors = errors;
			this.Warnings = warnings;
			this.Revision = revision;
		}
		#endregion

		#region Public Properties
		public IReadOnlyList<string> Errors { get; }

		public long Id { get; }

		public RevisionItem Revision { get; }

		public string Status { get; }

		public IReadOnlyList<string> Warnings { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Id.ToString(CultureInfo.CurrentCulture);
		#endregion
	}
}
