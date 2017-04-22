#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	public class SubscribedHooksItem
	{
		#region Public Properties
		public string Name { get; set; }

		public IReadOnlyList<string> Subscribers { get; set; }
		#endregion
	}
}
