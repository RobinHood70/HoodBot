namespace RobinHood70.WallE.Base
{
	/// <summary>Interface to support token generation (currently only api.php and index.php).</summary>
	public interface ITokenGenerator
	{
		/// <summary>Gets the class to use as a token manager.</summary>
		/// <value>The token manager.</value>
		ITokenManager TokenManager { get; }
	}
}