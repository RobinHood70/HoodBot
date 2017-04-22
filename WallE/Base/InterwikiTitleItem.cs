#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	public class InterwikiTitleItem
	{
		#region Public Properties
		public string InterwikiPrefix { get; set; }

		public string Title { get; set; }

		public Uri Url { get; set; }
		#endregion
	}
}
