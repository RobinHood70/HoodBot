﻿#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using static Properties.Messages;
	using static RobinHood70.Globals;

	public class RateLimitInfo
	{
		#region Public Properties
		public int Hits { get; set; }

		public int Seconds { get; set; }
		#endregion

		#region Public Override Methods
		public override string ToString() => CurrentCulture(PerText, this.Hits, this.Seconds);
		#endregion
	}
}
