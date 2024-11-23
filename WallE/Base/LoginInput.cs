#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System;

public class LoginInput
{
	#region Constructors
	public LoginInput(string userName, string password)
	{
		ArgumentNullException.ThrowIfNull(userName);
		ArgumentNullException.ThrowIfNull(password);
		this.UserName = userName;
		this.Password = password;
	}
	#endregion

	#region Public Properties
	public string? Domain { get; set; }

	public string Password { get; }

	public string? Token { get; set; }

	public string UserName { get; }
	#endregion
}