#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using RobinHood70.CommonCode;
using RobinHood70.WallE.Properties;

public class RateLimitInfo
{
	#region Constructors
	internal RateLimitInfo(int hits, int seconds)
	{
		this.Hits = hits;
		this.Seconds = seconds;
	}
	#endregion

	#region Public Properties
	public int Hits { get; }

	public int Seconds { get; }
	#endregion

	#region Public Override Methods
	public override string ToString() => Globals.CurrentCulture(Messages.PerText, this.Hits, this.Seconds);
	#endregion
}