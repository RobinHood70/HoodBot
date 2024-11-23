#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

public class DebugInfoInclude(string name, string size)
{
	#region Public Properties
	public string Name { get; } = name;

	public string Size { get; } = size;
	#endregion

	#region Public Override Methods
	public override string ToString() => $"{this.Name} ({this.Size})";
	#endregion
}