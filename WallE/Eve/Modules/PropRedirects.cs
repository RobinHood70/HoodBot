#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class PropRedirects : PropListModule<RedirectsInput, RedirectsItem>, IGeneratorModule
	{
		#region Constructors
		public PropRedirects(WikiAbstractionLayer wal, RedirectsInput input)
			: this(wal, input, null)
		{
		}

		public PropRedirects(WikiAbstractionLayer wal, RedirectsInput input, IPageSetGenerator pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 124;

		public override string Name { get; } = "redirects";
		#endregion

		#region Protected Override Properties
		protected override string Prefix { get; } = "rd";
		#endregion

		#region Public Static Methods
		public static PropRedirects CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new PropRedirects(wal, input as RedirectsInput, pageSetGenerator);

		public static PropRedirects CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new PropRedirects(wal, input as RedirectsInput);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, RedirectsInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddFlags("prop", input.Properties)
				.Add("namespace", input.Namespaces)
				.AddFilterPiped("show", "fragment", input.FilterFragments)
				.Add("limit", this.Limit);
		}

		protected override RedirectsItem GetItem(JToken result) => result == null
			? null
			: new RedirectsItem
			{
				Fragment = (string)result["fragment"]
			}.GetWikiTitle(result);

		protected override void GetResultsFromCurrentPage() => this.ResetItems(this.Output.Redirects);

		protected override void SetResultsOnCurrentPage() => this.Output.Redirects = this.CopyList();
		#endregion
	}
}