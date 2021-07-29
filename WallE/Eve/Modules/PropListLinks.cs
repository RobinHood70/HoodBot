#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	public abstract class PropListLinks<TInput> : PropListModule<TInput, ITitle>
		where TInput : class, ILinksInput
	{
		#region Constructors
		protected PropListLinks(WikiAbstractionLayer wal, TInput input)
			: this(wal, input, null)
		{
		}

		protected PropListLinks(WikiAbstractionLayer wal, TInput input, IPageSetGenerator? pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, TInput input)
		{
			input.ThrowNull(nameof(input));
			request
				.NotNull(nameof(request))
				.Add("namespace", input.Namespaces)
				.Add("dir", input.SortDescending)
				.Add("limit", this.Limit);
		}

		protected override ITitle GetItem(JToken result, PageItem page) => result.NotNull(nameof(result)).GetWikiTitle();
		#endregion
	}
}
