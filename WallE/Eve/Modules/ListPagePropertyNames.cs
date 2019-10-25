#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ListPagePropertyNames : ListModule<PagePropertyNamesInput, string>
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
		protected override void BuildRequestLocal(Request request, PagePropertyNamesInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request.Add("limit", this.Limit);
		}

		protected override string? GetItem(JToken result) => (string?)result?["propname"];
		#endregion
	}
}