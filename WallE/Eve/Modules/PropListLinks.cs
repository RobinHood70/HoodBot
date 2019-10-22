#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	public abstract class PropListLinks<TInput> : PropListModule<TInput, ITitle>
		where TInput : class, ILinksInput
	{
		#region Constructors
		protected PropListLinks(WikiAbstractionLayer wal, TInput input)
			: this(wal, input, null)
		{
		}

		protected PropListLinks(WikiAbstractionLayer wal, TInput input, IPageSetGenerator pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, TInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.Add("namespace", input.Namespaces)
				.Add("dir", input.SortDescending)
				.Add("limit", this.Limit);
		}

		protected override ITitle? GetItem(JToken result, PageItem page) => result?.GetWikiTitle();
		#endregion
	}
}
