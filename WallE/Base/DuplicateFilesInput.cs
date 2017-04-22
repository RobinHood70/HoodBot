#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class DuplicateFilesInput : IPropertyInput, ILimitableInput, IGeneratorInput
	{
		#region Public Properties
		public int Limit { get; set; }

		public bool LocalOnly { get; set; }

		public int MaxItems { get; set; }

		public bool SortDescending { get; set; }
		#endregion
	}
}
