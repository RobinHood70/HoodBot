#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class PropContributors : PropListModule<ContributorsInput, ContributorItem>
	{
		#region Constructors
		public PropContributors(WikiAbstractionLayer wal, ContributorsInput input)
			: base(wal, input, null)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 123;

		public override string Name { get; } = "contributors";
		#endregion

		#region Protected Override Properties
		protected override string Prefix { get; } = "pc";
		#endregion

		#region Public Static Methods
		public static PropContributors CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) =>
			input is ContributorsInput propInput
				? new PropContributors(wal, propInput)
				: throw InvalidParameterType(nameof(input), nameof(ContributorsInput), input.GetType().Name);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, ContributorsInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddIf(input.FilterType.ToString().ToLowerInvariant(), input.FilterValues, input.FilterType != ContributorsFilterType.None)
				.Add("limit", this.Limit);
		}

		protected override void DeserializeParentToPage(JToken parent, PageItem page)
		{
			ThrowNull(parent, nameof(parent));
			ThrowNull(page, nameof(page));
			page.AnonContributors = (int?)parent["anoncontributors"] ?? 0;
		}

		protected override ContributorItem? GetItem(JToken result, PageItem page) => result == null
			? null
			: new ContributorItem(result.MustHaveString("name"), (long)result.MustHave("userid"));

		protected override ICollection<ContributorItem> GetMutableList(PageItem page) => (ICollection<ContributorItem>)page.Contributors;
		#endregion
	}
}
