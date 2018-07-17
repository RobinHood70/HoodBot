﻿#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class PropCategories : PropListModule<CategoriesInput, CategoriesItem>, IGeneratorModule
	{
		#region Constructors
		public PropCategories(WikiAbstractionLayer wal, CategoriesInput input)
			: this(wal, input, null)
		{
		}

		public PropCategories(WikiAbstractionLayer wal, CategoriesInput input, IPageSetGenerator pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 111;

		public override string Name { get; } = "categories";
		#endregion

		#region Protected Override Properties
		protected override string Prefix { get; } = "cl";
		#endregion

		#region Public Static Methods
		public static PropCategories CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new PropCategories(wal, input as CategoriesInput, pageSetGenerator);

		public static PropCategories CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new PropCategories(wal, input as CategoriesInput);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, CategoriesInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddFlags("prop", input.Properties)
				.AddFilterPiped("show", "hidden", input.FilterHidden)
				.Add("categories", input.Categories)
				.AddIf("dir", "descending", input.SortDescending)
				.Add("limit", this.Limit);
		}

		protected override CategoriesItem GetItem(JToken result) => result == null
			? null
			: new CategoriesItem()
			{
				Namespace = (int?)result["ns"], // Should always be 14, but theoretically, an extension might cause other results to be possible, so read it in just in case.
				Title = (string)result["title"],
				SortKey = (string)result["sortkey"],
				SortKeyPrefix = (string)result["sortkeyprefix"],
				Timestamp = (DateTime?)result["timestamp"],
				Hidden = result["hidden"].AsBCBool(),
			};

		protected override void GetResultsFromCurrentPage() => this.ResetItems(this.Output.Categories);

		protected override void SetResultsOnCurrentPage() => this.Output.Categories = this.CopyList();
		#endregion
	}
}