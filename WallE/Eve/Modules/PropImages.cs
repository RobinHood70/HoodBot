#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class PropImages : PropListModule<ImagesInput, ITitle>, IGeneratorModule
	{
		#region Constructors
		public PropImages(WikiAbstractionLayer wal, ImagesInput input)
			: this(wal, input, null)
		{
		}

		public PropImages(WikiAbstractionLayer wal, ImagesInput input, IPageSetGenerator pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 111;

		public override string Name { get; } = "images";
		#endregion

		#region Protected Override Properties
		protected override string Prefix { get; } = "im";
		#endregion

		#region Public Static Methods
		public static PropImages CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new PropImages(wal, input as ImagesInput, pageSetGenerator);

		public static PropImages CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new PropImages(wal, input as ImagesInput);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, ImagesInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddIf("images", input.Images, this.SiteVersion >= 118)
				.AddIf("dir", "descending", input.SortDescending && this.SiteVersion >= 119)
				.Add("limit", this.Limit);
		}

		protected override ITitle GetItem(JToken result) => result.GetWikiTitle();

		protected override void GetResultsFromCurrentPage() => this.ResetItems(this.Output.Images);

		protected override void SetResultsOnCurrentPage() => this.Output.Images = this.Result();
		#endregion
	}
}