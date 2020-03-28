#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using RobinHood70.CommonCode;
	using RobinHood70.WikiCommon;
	using static RobinHood70.CommonCode.Globals;

	public class BacklinksInput : ILimitableInput, IGeneratorInput
	{
		#region Constructors
		public BacklinksInput(string title, BacklinksTypes linkTypes)
		{
			this.LinkTypes = linkTypes;
			this.Title = title;
		}

		public BacklinksInput(long pageId, BacklinksTypes linkTypes)
		{
			this.LinkTypes = linkTypes;
			this.PageId = pageId;
		}

		public BacklinksInput(BacklinksInput input, BacklinksTypes linkType)
		{
			ThrowNull(input, nameof(input));
			this.FilterRedirects = input.FilterRedirects;
			this.Limit = input.Limit;
			this.LinkTypes = linkType;
			this.MaxItems = input.MaxItems;
			this.PageId = input.PageId;
			this.Redirect = input.Redirect;
			this.SortDescending = input.SortDescending;
			this.Title = input.Title;
			this.Namespace = input.Namespace;
		}
		#endregion

		#region Public Properties
		public Filter FilterRedirects { get; set; }

		public int Limit { get; set; }

		public BacklinksTypes LinkTypes { get; }

		public int MaxItems { get; set; }

		public int? Namespace { get; set; }

		public long PageId { get; set; }

		public bool Redirect { get; set; }

		public bool SortDescending { get; set; }

		public string? Title { get; set; }
		#endregion
	}
}