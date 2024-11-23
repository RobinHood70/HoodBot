#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System.Collections.Generic;

public class ResetPasswordResult
{
	#region Constructors
	internal ResetPasswordResult(string status, IReadOnlyDictionary<string, string> passwords)
	{
		this.Status = status;
		this.Passwords = passwords;
	}
	#endregion

	#region Public Properties
	public IReadOnlyDictionary<string, string> Passwords { get; }

	public string Status { get; }
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Status;
	#endregion
}