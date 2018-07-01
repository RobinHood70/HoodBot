#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class PropCategoryInfo : PropModule<CategoryInfoInput>
	{
		#region Constructors
		public PropCategoryInfo(WikiAbstractionLayer wal, CategoryInfoInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Protected Internal Override Properties
		public override int MinimumVersion { get; } = 113;

		public override string Name { get; } = "categoryinfo";
		#endregion

		#region Public Override Properties
		protected override string Prefix { get; } = "ci";
		#endregion

		#region Public Static Methods
		public static PropCategoryInfo CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new PropCategoryInfo(wal, input as CategoryInfoInput);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, CategoryInfoInput input)
		{
		}

		protected override void DeserializeResult(JToken result, PageItem output)
		{
			ThrowNull(result, nameof(result));
			ThrowNull(output, nameof(output));

			var categoryInfo = new CategoryInfoResult()
			{
				Files = (int)result["files"],
				Pages = (int)result["pages"],
				Size = (int)result["size"],
				Subcategories = (int)result["subcats"],
				Hidden = result["hidden"].AsBCBool(),
			};
			output.CategoryInfo = categoryInfo;
		}
		#endregion
	}
}