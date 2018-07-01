#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.WikiCommon;

	#region Public Enumerations
	[Flags]
	public enum FileUsageProperties
	{
		None = 0,
		PageId = 1,
		Title = 1 << 1,
		Redirect = 1 << 2
	}
	#endregion

	public class FileUsageInput : IPropertyInput, ILimitableInput, IGeneratorInput
	{
		#region Public Properties
		public Filter FilterRedirects { get; set; }

		public int Limit { get; set; }

		public int MaxItems { get; set; }

		public IEnumerable<int> Namespaces { get; set; }

		public FileUsageProperties Properties { get; set; }
		#endregion
	}
}
