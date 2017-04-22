#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	[Flags]
	public enum InterwikiLinksProperties
	{
		None = 0,
		Url = 1
	}

	public class InterwikiLinksInput : IPropertyInput, ILimitableInput
	{
		#region Public Properties
		public int Limit { get; set; }

		public int MaxItems { get; set; }

		public string Prefix { get; set; }

		public InterwikiLinksProperties Properties { get; set; }

		public bool SortDescending { get; set; }

		public string Title { get; set; }
		#endregion
	}
}
