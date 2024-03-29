﻿namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;

	internal sealed class ListPagePropertyNames : ListModule<PagePropertyNamesInput, string>
	{
		#region Constructors
		public ListPagePropertyNames(WikiAbstractionLayer wal, PagePropertyNamesInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 117;

		public override string Name => "pagepropnames";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "ppn";
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, PagePropertyNamesInput input) => request
			.NotNull()
			.Add("limit", this.Limit);

		protected override string? GetItem(JToken result) => (string?)result?["propname"];
		#endregion
	}
}