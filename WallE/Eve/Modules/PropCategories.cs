#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Base;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static RobinHood70.Globals;

	internal class PropCategories : PropListModule<CategoriesInput, CategoriesItem>, IGeneratorModule
	{
		#region Constructors
		public PropCategories(WikiAbstractionLayer wal, CategoriesInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Protected Internal Override Properties
		public override int MinimumVersion { get; } = 111;

		public override string Name { get; } = "categories";
		#endregion

		#region Public Override Properties
		protected override string BasePrefix { get; } = "cl";
		#endregion

		#region Public Static Methods
		public static PropCategories CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input) => new PropCategories(wal, input as CategoriesInput);

		public static PropCategories CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new PropCategories(wal, input as CategoriesInput);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, CategoriesInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddFlags("prop", input.Properties)
				.AddFilterOptionPiped("show", "hidden", input.FilterHidden)
				.Add("categories", input.Categories)
				.AddIf("dir", "descending", input.SortDescending)
				.Add("limit", this.Limit);
		}

		protected override CategoriesItem GetItem(JToken result)
		{
			if (result == null)
			{
				return null;
			}

			var item = new CategoriesItem()
			{
				Namespace = (int?)result["ns"], // Should always be 14, but theoretically, an extension might cause other results to be possible, so read it in just in case.
				Title = (string)result["title"],
				SortKey = (string)result["sortkey"],
				SortKeyPrefix = (string)result["sortkeyprefix"],
				Timestamp = (DateTime?)result["timestamp"],
				Hidden = result["hidden"].AsBCBool(),
			};
			return item;
		}

		protected override void GetResultsFromCurrentPage() => this.ResetMyList(this.Output.Categories);

		protected override void SetResultsOnCurrentPage() => this.Output.Categories = this.MyList.AsNewReadOnlyList();
		#endregion
	}
}