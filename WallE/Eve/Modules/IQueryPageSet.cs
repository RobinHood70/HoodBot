#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;

	internal interface IQueryPageSet : IPageSetGenerator
	{
		#region Properties
		HashSet<string> InactiveModules { get; }
		#endregion
	}
}
