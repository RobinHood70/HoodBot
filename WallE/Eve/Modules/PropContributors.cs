namespace RobinHood70.WallE.Eve.Modules;

using System;
using Newtonsoft.Json.Linq;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon.RequestBuilder;
using static RobinHood70.WallE.Eve.ParsingExtensions;

internal sealed class PropContributors(WikiAbstractionLayer wal, ContributorsInput input) : PropListModule<ContributorsInput, ContributorsResult, ContributorsItem>(wal, input, null)
{
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
		ArgumentNullException.ThrowIfNull(request);
		ArgumentNullException.ThrowIfNull(input);
		request
			.AddIf(input.FilterType.ToString().ToLowerInvariant(), input.FilterValues, input.FilterType != ContributorsFilterType.None)
			.Add("limit", this.Limit);
	}

	protected override ContributorsItem? GetItem(JToken result) => result == null
		? null
		: new ContributorsItem(result.MustHaveString("name"), (long)result.MustHave("userid"));

	protected override ContributorsResult GetNewList(JToken parent)
	{
		ArgumentNullException.ThrowIfNull(parent);
		return new ContributorsResult((int?)parent["anoncontributors"] ?? 0);
	}
	#endregion
}