#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Base;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using WikiCommon;
	using static WikiCommon.Globals;

	internal class PropRedirects : PropListModule<RedirectsInput, RedirectsItem>, IGeneratorModule
	{
		#region Constructors
		public PropRedirects(WikiAbstractionLayer wal, RedirectsInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Protected Internal Override Properties
		public override int MinimumVersion { get; } = 124;

		public override string Name { get; } = "redirects";
		#endregion

		#region Public Override Properties
		protected override string BasePrefix { get; } = "rd";
		#endregion

		#region Public Static Methods
		public static PropRedirects CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input) => new PropRedirects(wal, input as RedirectsInput);

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

		protected override RedirectsItem GetItem(JToken result)
		{
			if (result == null)
			{
				return null;
			}

			var item = new RedirectsItem();
			item.GetWikiTitle(result);
			item.Fragment = (string)result["fragment"];

			return item;
		}

		protected override void GetResultsFromCurrentPage() => this.ResetMyList(this.Output.Redirects);

		protected override void SetResultsOnCurrentPage() => this.Output.Redirects = this.MyList.AsNewReadOnlyList();
		#endregion
	}
}