namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.RequestBuilder;

	internal sealed class PropLinks(WikiAbstractionLayer wal, LinksInput input, IPageSetGenerator? pageSetGenerator) : PropListModule<LinksInput, LinksResult, IApiTitle>(wal, input, pageSetGenerator), IGeneratorModule
	{
		#region Constructors
		public PropLinks(WikiAbstractionLayer wal, LinksInput input)
			: this(wal, input, null)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 111;

		public override string Name => "links";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "pl";
		#endregion

		#region Public Static Methods
		public static PropLinks CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new(wal, (LinksInput)input, pageSetGenerator);

		public static PropLinks CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new(wal, (LinksInput)input);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, LinksInput input)
		{
			ArgumentNullException.ThrowIfNull(request);
			ArgumentNullException.ThrowIfNull(input);
			request
				.Add("titles", input.Titles)
				.Add("namespace", input.Namespaces)
				.Add("dir", input.SortDescending)
				.Add("limit", this.Limit);
		}

		protected override IApiTitle GetItem(JToken result) => result.NotNull().GetWikiTitle();

		protected override LinksResult GetNewList(JToken parent) => [];
	}
	#endregion
}