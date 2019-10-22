#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class RateLimitsItem
	{
		#region Constructors
		internal RateLimitsItem(RateLimitInfo? anonymous, RateLimitInfo? ip, RateLimitInfo? newbie, RateLimitInfo? subnet, RateLimitInfo? user)
		{
			this.Anonymous = anonymous;
			this.IP = ip;
			this.Newbie = newbie;
			this.Subnet = subnet;
			this.User = user;
		}
		#endregion

		#region Public Properties
		public RateLimitInfo? Anonymous { get; }

		public RateLimitInfo? IP { get; }

		public RateLimitInfo? Newbie { get; }

		public RateLimitInfo? Subnet { get; }

		public RateLimitInfo? User { get; }
		#endregion
	}
}
