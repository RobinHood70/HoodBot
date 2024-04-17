namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class PropCategories : PropListModule<CategoriesInput, CategoriesItem>, IGeneratorModule
	{
		#region Constructors
		public PropCategories(WikiAbstractionLayer wal, CategoriesInput input)
			: this(wal, input, null)
		{
		}

		public PropCategories(WikiAbstractionLayer wal, CategoriesInput input, IPageSetGenerator? pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 111;

		public override string Name => "categories";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "cl";
		#endregion

		#region Public Static Methods
		public static PropCategories CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new(wal, (CategoriesInput)input, pageSetGenerator);

		public static PropCategories CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new(wal, (CategoriesInput)input);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, CategoriesInput input)
		{
			input.ThrowNull();
			request
				.NotNull()
				.AddFlags("prop", input.Properties)
				.AddFilterPiped("show", "hidden", input.FilterHidden)
				.Add("categories", input.Categories)
				.AddIf("dir", "descending", input.SortDescending)
				.Add("limit", this.Limit);
		}

		protected override CategoriesItem? GetItem(JToken result, PageItem page) => result == null
			? null
			: new CategoriesItem(
				ns: (int)result.MustHave("ns"),
				title: result.MustHaveString("title"),
				hidden: result["hidden"].GetBCBool(),
				sortkey: (string?)result["sortkey"],
				sortkeyPrefix: (string?)result["sortkeyprefix"],
				timestamp: (DateTime?)result["timestamp"]);

		protected override IList<CategoriesItem> GetMutableList(PageItem page) => page.Categories;
		#endregion
	}
}