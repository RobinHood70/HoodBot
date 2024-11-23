#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System.Collections.Generic;

public class SiteInfoUserGroup
{
	#region Constructors
	internal SiteInfoUserGroup(string name, IReadOnlyList<string> rights, long number, IReadOnlyList<string>? add, IReadOnlyList<string>? addSelf, IReadOnlyList<string>? remove, IReadOnlyList<string>? removeSelf)
	{
		this.Name = name;
		this.Rights = rights;
		this.Number = number;
		this.Add = add;
		this.AddSelf = addSelf;
		this.Remove = remove;
		this.RemoveSelf = removeSelf;
	}
	#endregion

	#region Public Properties
	public IReadOnlyList<string>? Add { get; }

	public IReadOnlyList<string>? AddSelf { get; }

	public string Name { get; }

	public long Number { get; }

	public IReadOnlyList<string>? Remove { get; }

	public IReadOnlyList<string>? RemoveSelf { get; }

	public IReadOnlyList<string> Rights { get; }
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Name;
	#endregion
}