namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class ActionRevisionDelete : ActionModule<RevisionDeleteInput, RevisionDeleteResult>
	{
		#region Constructors
		public ActionRevisionDelete(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 123;

		public override string Name => "revisiondelete";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType => RequestType.Post;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, RevisionDeleteInput input)
		{
			input.ThrowNull();
			request
				.NotNull()
				.Add("type", input.Type)
				.AddIfNotNull("target", input.Target)
				.Add("ids", input.Ids)
				.AddFlags("hide", input.Hide)
				.AddFlags("show", input.Show)
				.AddIfPositive("suppress", input.Suppress)
				.AddIfNotNull("reason", input.Reason)
				.AddHidden("token", input.Token);
		}

		protected override RevisionDeleteResult DeserializeResult(JToken? result)
		{
			result.ThrowNull();
			List<RevisionDeleteItem> list = [];
			foreach (var item in result.MustHave("items"))
			{
				var revision = item.GetRevision();
				list.Add(new RevisionDeleteItem(
					status: item.MustHaveString("status"),
					id: (long)item.MustHave("id"),
					errors: GetRenderedMessages(item["errors"]),
					warnings: GetRenderedMessages(item["warnings"]),
					revision: revision));
			}

			return new RevisionDeleteResult(
				list: list,
				status: result.MustHaveString("status"),
				target: result.MustHaveString("target"),
				errors: GetRenderedMessages(result["errors"]),
				warnings: GetRenderedMessages(result["warnings"]));
		}
		#endregion

		#region Private Static Methods
		private static IReadOnlyList<string> GetRenderedMessages(JToken? messages)
		{
			List<string> output = [];
			if (messages != null)
			{
				foreach (var msg in messages)
				{
					// CONSIDER: Expand to include non-rendered message? Always emitted.
					output.Add(msg.MustHaveString("rendered"));
				}
			}

			return output.AsReadOnly();
		}
		#endregion
	}
}