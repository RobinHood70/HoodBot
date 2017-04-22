#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class RateLimitsItem
	{
		#region Public Properties
		public RateLimitInfo Anonymous { get; set; }

		public RateLimitInfo IP { get; set; }

		public RateLimitInfo Newbie { get; set; }

		public RateLimitInfo Subnet { get; set; }

		public RateLimitInfo User { get; set; }
		#endregion
	}
}
