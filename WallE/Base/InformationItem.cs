#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	// Hey, don't blame me for this naming! This is what you get when you create a paraminfo module for documenting info about parameters in modules! :)
	public class InformationItem
	{
		#region Public Properties
		public string Name { get; set; }

		public RawMessageInfo Text { get; set; }

		public IReadOnlyList<int> Values { get; set; }
		#endregion
	}
}
