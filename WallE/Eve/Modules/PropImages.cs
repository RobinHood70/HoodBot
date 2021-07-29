namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class PropImages : PropListModule<ImagesInput, IApiTitle>, IGeneratorModule
	{
		#region Constructors
		public PropImages(WikiAbstractionLayer wal, ImagesInput input)
			: this(wal, input, null)
		{
		}

		public PropImages(WikiAbstractionLayer wal, ImagesInput input, IPageSetGenerator? pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 111;

		public override string Name => "images";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "im";
		#endregion

		#region Public Static Methods
		public static PropImages CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new(wal, (ImagesInput)input, pageSetGenerator);

		public static PropImages CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new(wal, (ImagesInput)input);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, ImagesInput input)
		{
			input.ThrowNull(nameof(input));
			request
				.NotNull(nameof(request))
				.AddIf("images", input.Images, this.SiteVersion >= 118)
				.AddIf("dir", "descending", input.SortDescending && this.SiteVersion >= 119)
				.Add("limit", this.Limit);
		}

		protected override IApiTitle GetItem(JToken result, PageItem page) => result.GetWikiTitle();

		protected override ICollection<IApiTitle> GetMutableList(PageItem page) => (ICollection<IApiTitle>)page.Images;
		#endregion
	}
}