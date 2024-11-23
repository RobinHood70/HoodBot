#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System.Collections.Generic;

public class ExtendedMetadataItem
{
	#region Constructors
	internal ExtendedMetadataItem(IReadOnlyDictionary<string, string> values, string source, bool hidden)
	{
		this.MultilanguageValues = values;
		this.Source = source;
		this.Hidden = hidden;
	}
	#endregion

	#region Public Properties
	public bool Hidden { get; }

	public IReadOnlyDictionary<string, string> MultilanguageValues { get; }

	public string Source { get; }
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Source;
	#endregion
}