#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	#region Public Enumerations
	public enum ContributorsFilterType
	{
		None,
		ExcludeGroup,
		ExcludeRights,
		Group,
		Rights
	}
	#endregion

	public class ContributorsInput : IPropertyInput, ILimitableInput
	{
		#region Public Properties
		public ContributorsFilterType FilterType { get; set; }

		public IEnumerable<string> FilterValues { get; set; }

		public int Limit { get; set; }

		public int MaxItems { get; set; }
		#endregion
	}
}
