namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class ListAllCategories : ListModule<AllCategoriesInput, AllCategoriesItem>, IGeneratorModule
	{
		#region Constructors
		public ListAllCategories(WikiAbstractionLayer wal, AllCategoriesInput input)
			: this(wal, input, null)
		{
		}

		public ListAllCategories(WikiAbstractionLayer wal, AllCategoriesInput input, IPageSetGenerator? pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 112;

		public override string Name => "allcategories";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "ac";
		#endregion

		#region Public Static Methods
		public static ListAllCategories CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new(wal, (AllCategoriesInput)input, pageSetGenerator);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, AllCategoriesInput input)
		{
			ArgumentNullException.ThrowIfNull(input);
			ArgumentNullException.ThrowIfNull(request);
			request
				.AddIfNotNull("from", input.From)
				.AddIfNotNull("to", input.To)
				.AddIfNotNull("prefix", input.Prefix)
				.AddIf("dir", "descending", input.SortDescending)
				.AddIf("min", input.MinCount, input.MinCount >= 0)
				.AddIf("max", input.MaxCount, input.MaxCount >= 0)
				.AddFlags("prop", input.Properties)
				.Add("limit", this.Limit);
		}

		protected override AllCategoriesItem GetItem(JToken result)
		{
			ArgumentNullException.ThrowIfNull(result);
			return new AllCategoriesItem(
				category: result.MustHaveBCString("category"),
				files: (int?)result["files"] ?? 0,
				hidden: result["hidden"].GetBCBool(),
				pages: (int?)result["pages"] ?? 0,
				size: (int?)result["size"] ?? 0,
				subcats: (int?)result["subcats"] ?? 0);
		}
		#endregion
	}
}