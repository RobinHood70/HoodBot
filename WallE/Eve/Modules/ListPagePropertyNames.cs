#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Base;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static WikiCommon.Globals;

	internal class ListPagePropertyNames : ListModule<PagePropertyNamesInput, string>
	{
		#region Constructors
		public ListPagePropertyNames(WikiAbstractionLayer wal, PagePropertyNamesInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Protected Internal Override Properties
		public override int MinimumVersion { get; } = 117;

		public override string Name { get; } = "pagepropnames";
		#endregion

		#region Public Override Properties
		protected override string BasePrefix { get; } = "ppn";
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, PagePropertyNamesInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request.Add("limit", this.Limit);
		}

		protected override string GetItem(JToken result) => (string)result?["propname"];
		#endregion
	}
}