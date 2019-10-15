#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ActionMergeHistory : ActionModule<MergeHistoryInput, MergeHistoryResult>
	{
		#region Constructors
		public ActionMergeHistory(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 127;

		public override string Name { get; } = "mergehistory";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType { get; } = RequestType.Post;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, MergeHistoryInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddIfNotNull("from", input.From)
				.AddIfPositive("fromid", input.FromId)
				.AddIfNotNull("to", input.To)
				.AddIfPositive("toid", input.ToId)
				.AddIfNotNull("reason", input.Reason)
				.Add("timestamp", input.Timestamp)
				.AddHidden("token", input.Token);
		}

		protected override MergeHistoryResult DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			var output = new MergeHistoryResult()
			{
				From = (string?)result["from"],
				To = (string?)result["to"],
				Reason = (string?)result["reason"],
				Timestamp = result["timestamp"].AsDate(),
			};
			return output;
		}
		#endregion
	}
}
