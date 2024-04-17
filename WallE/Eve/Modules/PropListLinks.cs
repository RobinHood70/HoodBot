#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	public abstract class PropListLinks<TInput> : PropListModule<TInput, IApiTitle>
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
			input.ThrowNull();
			request
				.NotNull()
				.Add("namespace", input.Namespaces)
				.Add("dir", input.SortDescending)
				.Add("limit", this.Limit);
		}

		protected override IApiTitle GetItem(JToken result) => result.NotNull().GetWikiTitle();
		#endregion
	}
}