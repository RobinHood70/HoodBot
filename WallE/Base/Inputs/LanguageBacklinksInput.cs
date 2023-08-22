#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	#region Public Enumerations
	[Flags]
	public enum LanguageBacklinksProperties
	{
		None = 0,
		LLLang = 1,
		LLTitle = 1 << 1,
		All = LLLang | LLTitle
	}
	#endregion

	public class LanguageBacklinksInput : ILimitableInput, IGeneratorInput
	{
		#region Constructors
		public LanguageBacklinksInput(string language)
		{
			this.Language = language;
		}
		#endregion

		#region public Properties
		public string Language { get; }

		public int Limit { get; set; }

		public int MaxItems { get; set; }

		public LanguageBacklinksProperties Properties { get; set; }

		public bool SortDescending { get; set; }

		public string? Title { get; set; }
		#endregion
	}
}