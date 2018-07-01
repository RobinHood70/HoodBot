#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using RobinHood70.WikiCommon;

	public class AllPagesInput : ILimitableInput, IGeneratorInput
	{
		#region Public Properties
		public Filter FilterCascading { get; set; }

		public Filter FilterIndefinite { get; set; }

		public Filter FilterLanguageLinks { get; set; }

		public Filter FilterRedirects { get; set; }

		public string From { get; set; }

		public int Limit { get; set; }

		public int MaxItems { get; set; }

		public int MaximumSize { get; set; } = -1;

		public int MinimumSize { get; set; } = -1;

		public int? Namespace { get; set; }

		public string Prefix { get; set; }

		public string ProtectionLevel { get; set; }

		public string ProtectionType { get; set; }

		public bool SortDescending { get; set; }

		public string To { get; set; }
		#endregion
	}
}
