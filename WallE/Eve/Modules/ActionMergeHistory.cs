﻿namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class ActionMergeHistory : ActionModule<MergeHistoryInput, MergeHistoryResult>
	{
		#region Constructors
		public ActionMergeHistory(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 127;

		public override string Name => "mergehistory";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType => RequestType.Post;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, MergeHistoryInput input)
		{
			input.ThrowNull(nameof(input));
			request
				.NotNull(nameof(request))
				.AddIfNotNull("from", input.From)
				.AddIfPositive("fromid", input.FromId)
				.AddIfNotNull("to", input.To)
				.AddIfPositive("toid", input.ToId)
				.AddIfNotNull("reason", input.Reason)
				.Add("timestamp", input.Timestamp)
				.AddHidden("token", input.Token);
		}

		protected override MergeHistoryResult DeserializeResult(JToken? result)
		{
			result.ThrowNull(nameof(result));
			return new MergeHistoryResult(
				from: result.MustHaveString("from"),
				reason: result.MustHaveString("reason"),
				timestamp: result.MustHaveDate("timestamp"),
				to: result.MustHaveString("to"));
		}
		#endregion
	}
}
