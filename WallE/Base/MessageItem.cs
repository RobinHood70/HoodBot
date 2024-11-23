#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System.Collections.Generic;

public class MessageItem
{
	#region Constructors
	internal MessageItem(string key, IReadOnlyList<string> parameters, string? forValue)
	{
		this.Key = key;
		this.Parameters = parameters;
		this.ForValue = forValue;
	}
	#endregion

	#region Public Properties
	public string? ForValue { get; }

	public string Key { get; }

	public IReadOnlyList<string> Parameters { get; }
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Key;
	#endregion
}