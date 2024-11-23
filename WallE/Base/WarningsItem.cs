#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System.Collections.Generic;

// This is mostly internal to MediaWiki, but CreateAccount returns it as part of its warnings.
public class WarningsItem
{
	#region Constructors
	internal WarningsItem(string type, string? message, IReadOnlyList<object> parameters)
	{
		this.Type = type;
		this.Message = message;
		this.Parameters = parameters;
	}
	#endregion

	#region Public Properties
	public string? Message { get; }

	public IReadOnlyList<object> Parameters { get; }

	public string Type { get; }
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Type;
	#endregion
}