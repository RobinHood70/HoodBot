#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System.Globalization;

public class SiteInfoLag
{
	#region Constructors
	internal SiteInfoLag(string host, int lag)
	{
		this.Host = host;
		this.Lag = lag;
	}
	#endregion

	#region Public Properties
	public string Host { get; }

	public int Lag { get; }
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Host + " (" + this.Lag.ToString(CultureInfo.CurrentCulture) + "s)";
	#endregion
}