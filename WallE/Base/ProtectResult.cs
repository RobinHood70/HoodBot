#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System.Collections.Generic;
using RobinHood70.WikiCommon;

public class ProtectResult : IApiTitle
{
	#region Constructors
	internal ProtectResult(int ns, string title, string reason, bool cascade, IReadOnlyList<ProtectResultItem> protections)
	{
		this.Namespace = ns;
		this.Title = title;
		this.Reason = reason;
		this.Cascade = cascade;
		this.Protections = protections;
	}
	#endregion

	#region Public Properties
	public bool Cascade { get; }

	public int Namespace { get; }

	public IReadOnlyList<ProtectResultItem> Protections { get; }

	public string Reason { get; }

	public string Title { get; }
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Title;
	#endregion
}