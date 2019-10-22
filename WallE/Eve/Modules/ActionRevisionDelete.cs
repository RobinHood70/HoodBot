#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ActionRevisionDelete : ActionModule<RevisionDeleteInput, RevisionDeleteResult>
	{
		#region Constructors
		public ActionRevisionDelete(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 123;

		public override string Name { get; } = "revisiondelete";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType { get; } = RequestType.Post;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, RevisionDeleteInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.Add("type", input.Type)
				.AddIfNotNull("target", input.Target)
				.Add("ids", input.Ids)
				.AddFlags("hide", input.Hide)
				.AddFlags("show", input.Show)
				.AddIfPositive("suppress", input.Suppress)
				.AddIfNotNull("reason", input.Reason)
				.AddHidden("token", input.Token);
		}

		protected override RevisionDeleteResult DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			var list = new List<RevisionDeleteItem>();
			foreach (var item in result.MustHave("items"))
			{
				var revision = item.GetRevision();
				list.Add(new RevisionDeleteItem(
					id: (long)item.MustHave("id"),
					status: item.MustHaveString("status"),
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
			var output = new List<string>();
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
