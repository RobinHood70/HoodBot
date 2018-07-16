#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.RequestBuilder;
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
			var items = result["items"];
			if (items != null)
			{
				foreach (var item in items)
				{
					var rdi = new RevisionDeleteItem()
					{
						Status = (string)item["status"],
						Id = (long)item["id"],
						Errors = GetRenderedMessages(item["errors"]),
						Warnings = GetRenderedMessages(item["warnings"]),
					};
					list.Add(rdi);
				}
			}

			var output = new RevisionDeleteResult(list)
			{
				Status = (string)result["status"],
				Target = (string)result["target"],
				Errors = GetRenderedMessages(result["errors"]),
				Warnings = GetRenderedMessages(result["warnings"]),
			};
			return output;
		}
		#endregion

		#region Private Static Methods
		private static IReadOnlyList<string> GetRenderedMessages(JToken messages)
		{
			var output = new List<string>();
			if (messages != null)
			{
				foreach (var msg in messages)
				{
					output.Add((string)msg["rendered"]);
				}
			}

			return output.AsReadOnly();
		}
		#endregion
	}
}
