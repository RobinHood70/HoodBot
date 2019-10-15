#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	#region Public Enumerations
	[Flags]
	public enum LanguageLinksProperties
	{
		None = 0,
		Url = 1,
		LangName = 1 << 1,
		Autonym = 1 << 2,
		All = Url | LangName | Autonym
	}
	#endregion

	public class LanguageLinksInput : IPropertyInput, ILimitableInput
	{
		#region Public Properties
		public string? InLanguageCode { get; set; }

		public string? Language { get; set; }

		public int Limit { get; set; }

		public int MaxItems { get; set; }

		public LanguageLinksProperties Properties { get; set; }

		public bool SortDescending { get; set; }

		public string? Title { get; set; }
		#endregion
	}
}
