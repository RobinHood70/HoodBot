#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System.Collections.Generic;

public class CreateAccountInput(string name, string password)
{
	#region Public Properties
	public IDictionary<string, string> CaptchaSolution { get; } = new Dictionary<string, string>(System.StringComparer.Ordinal);

	public string? Domain { get; set; }

	public string? Email { get; set; }

	public string? Language { get; set; }

	public bool MailPassword => this.Password == null && this.Email != null;

	public string Name { get; } = name;

	public string? Password { get; } = password;

	public string? RealName { get; set; }

	public string? Reason { get; set; }

	public string? Token { get; set; }
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Name;
	#endregion
}