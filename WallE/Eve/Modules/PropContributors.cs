#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class PropContributors : PropListModule<ContributorsInput, ContributorItem>
	{
		#region Constructors
		public PropContributors(WikiAbstractionLayer wal, ContributorsInput input)
			: base(wal, input)
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
		public static PropContributors CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new PropContributors(wal, input as ContributorsInput);
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

		protected override void DeserializeParent(JToken parent, PageItem output)
		{
			ThrowNull(parent, nameof(parent));
			ThrowNull(output, nameof(output));
			output.AnonContributors = (int?)parent["anoncontributors"] ?? 0;
		}

		protected override ContributorItem GetItem(JToken result) => result == null
			? null
			: new ContributorItem()
			{
				Name = (string)result["name"],
				UserId = (long)result["userid"],
			};

		protected override void GetResultsFromCurrentPage() => this.ResetItems(this.Output.Contributors);

		protected override void SetResultsOnCurrentPage() => this.Output.Contributors = this.Result();
		#endregion
	}
}
