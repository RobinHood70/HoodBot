#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System.Collections.Generic;

// IMPNOTE: OptionName and OptionValue are not necessary here - implementers are expected to evaluate the Change values and adjust accordingly.
public class OptionsInput
{
	#region Public Properties
	public IReadOnlyDictionary<string, string?>? Change { get; }

	public bool Reset { get; set; }

	public IEnumerable<string>? ResetKinds { get; set; }

	public string? Token { get; set; }
	#endregion
}