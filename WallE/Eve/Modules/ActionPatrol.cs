﻿namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class ActionPatrol : ActionModule<PatrolInput, PatrolResult>
	{
		#region Constructors
		public ActionPatrol(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

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
			input.ThrowNull(nameof(input));
			request
				.NotNull(nameof(request))
				.AddIfPositive("rcid", input.RecentChangesId)
				.AddIfPositive("revid", input.RevisionId)
				.Add("tags", input.Tags)
				.AddHidden("token", input.Token);
		}

		protected override PatrolResult DeserializeResult(JToken? result)
		{
			result.ThrowNull(nameof(result));
			return new PatrolResult(
				ns: (int)result.MustHave("ns"),
				title: result.MustHaveString("title"),
				rcId: (long)result.MustHave("rcid"));
		}
		#endregion
	}
}
