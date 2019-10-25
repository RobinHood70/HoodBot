#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class PropCategoryInfo : PropModule<CategoryInfoInput>
	{
		#region Constructors
		public PropCategoryInfo(WikiAbstractionLayer wal, CategoryInfoInput input)
			: base(wal, input, null)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 113;

		public override string Name => "categoryinfo";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "ci";
		#endregion

		#region Public Static Methods
		public static PropCategoryInfo CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) =>
			input is CategoryInfoInput propInput
				? new PropCategoryInfo(wal, propInput)
				: throw InvalidParameterType(nameof(input), nameof(CategoryInfoInput), input.GetType().Name);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, CategoryInfoInput input)
		{
		}

		protected override void DeserializeToPage(JToken result, PageItem page)
		{
			ThrowNull(result, nameof(result));
			page.CategoryInfo = new CategoryInfoResult(
				files: (int)result.MustHave("files"),
				pages: (int)result.MustHave("pages"),
				size: (int)result.MustHave("size"),
				subcategories: (int)result.MustHave("subcats"),
				hidden: result["hidden"].ToBCBool());
		}
		#endregion
	}
}