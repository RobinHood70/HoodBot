#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	public class PagePropertiesInput : IPropertyInput
	{
		#region Public Properties
		public IEnumerable<string>? Properties { get; set; }
		#endregion
	}
}