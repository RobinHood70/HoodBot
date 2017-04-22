#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	public class ProtectResult
	{
		#region Public Properties
		public bool Cascade { get; set; }

		public IReadOnlyList<ProtectResultItem> Protections { get; set; }

		public string Reason { get; set; }

		public string Title { get; set; }
		#endregion
	}
}
