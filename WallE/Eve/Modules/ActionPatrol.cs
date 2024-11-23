namespace RobinHood70.WallE.Eve.Modules;

using System;
using Newtonsoft.Json.Linq;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon.RequestBuilder;
using static RobinHood70.WallE.Eve.ParsingExtensions;

internal sealed class ActionPatrol(WikiAbstractionLayer wal) : ActionModule<PatrolInput, PatrolResult>(wal)
{
	#region Public Override Properties
	public override int MinimumVersion => 114;

	public override string Name => "patrol";
	#endregion

	#region Protected Override Properties
	protected override RequestType RequestType => RequestType.Post;
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestLocal(Request request, PatrolInput input)
	{
		ArgumentNullException.ThrowIfNull(input);
		ArgumentNullException.ThrowIfNull(request);
		request
			.AddIfPositive("rcid", input.RecentChangesId)
			.AddIfPositive("revid", input.RevisionId)
			.Add("tags", input.Tags)
			.AddHidden("token", input.Token);
	}

	protected override PatrolResult DeserializeResult(JToken? result)
	{
		ArgumentNullException.ThrowIfNull(result);
		return new PatrolResult(
			ns: (int)result.MustHave("ns"),
			title: result.MustHaveString("title"),
			rcId: (long)result.MustHave("rcid"));
	}
	#endregion
}