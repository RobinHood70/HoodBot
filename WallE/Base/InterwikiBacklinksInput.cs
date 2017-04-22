#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	#region Public Enumerations
	[Flags]
	public enum InterwikiBacklinksProperties
	{
		None = 0,
		IWPrefix = 1,
		IWTitle = 1 << 1,
		All = IWPrefix | IWTitle
	}
	#endregion

	public class InterwikiBacklinksInput : ILimitableInput, IGeneratorInput
	{
		#region Constructors
		public InterwikiBacklinksInput(string prefix) => this.Prefix = prefix;
		#endregion

		#region Public Properties
		public int Limit { get; set; }

		public int MaxItems { get; set; }

		public string Prefix { get; }

		public InterwikiBacklinksProperties Properties { get; set; }

		public bool SortDescending { get; set; }

		public string Title { get; set; }
		#endregion
	}
}
