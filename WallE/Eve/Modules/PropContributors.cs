namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class PropContributors : PropListModule<ContributorsInput, ContributorItem>
	{
		#region Constructors
		public PropContributors(WikiAbstractionLayer wal, ContributorsInput input)
			: base(wal, input, null)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 123;

		public override string Name => "contributors";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "pc";
		#endregion

		#region Public Static Methods
		public static PropContributors CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new(wal, (ContributorsInput)input);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, ContributorsInput input)
		{
			input.ThrowNull();
			request
				.NotNull()
				.AddIf(input.FilterType.ToString().ToLowerInvariant(), input.FilterValues, input.FilterType != ContributorsFilterType.None)
				.Add("limit", this.Limit);
		}

		protected override void DeserializeParentToPage(JToken parent, PageItem page) => page
			.NotNull()
			.AnonContributors = (int?)parent
				.NotNull()["anoncontributors"] ?? 0;

		protected override ContributorItem? GetItem(JToken result, PageItem page) => result == null
			? null
			: new ContributorItem(result.MustHaveString("name"), (long)result.MustHave("userid"));

		protected override ICollection<ContributorItem> GetMutableList(PageItem page) => (ICollection<ContributorItem>)page.Contributors;
		#endregion
	}
}
