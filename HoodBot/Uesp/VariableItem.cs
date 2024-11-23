namespace RobinHood70.HoodBot.Uesp;

using System;
using System.Collections.Generic;

public class VariableItem(IReadOnlyDictionary<string, string> dictionary, string? set)
{
	#region Public Properties
	public IReadOnlyDictionary<string, string> Dictionary { get; } = dictionary ?? throw new ArgumentNullException(nameof(dictionary));

	public string? Set { get; } = set;
	#endregion
}