#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ActionTag : ActionModuleValued<TagInput, List<TagItem>>
	{
		#region Constructors
		public ActionTag(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 125;

		public override string Name => "tag";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType => RequestType.Post;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, TagInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.Add("rcid", input.RecentChangesIds)
				.Add("revid", input.RevisionIds)
				.Add("logid", input.LogIds)
				.Add("add", input.Add)
				.Add("remove", input.Remove)
				.AddIfNotNull("reason", input.Reason)
				.AddHidden("token", input.Token);
		}

		protected override List<TagItem> DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			var tags = new List<TagItem>();
			if (result.Type == JTokenType.Array)
			{
				foreach (var item in result)
				{
					tags.Add(new TagItem(
						status: item.MustHaveString("status"),
						actionLogId: (long?)item["actionlogid"] ?? 0,
						added: item["added"].ToReadOnlyList<string>(),
						error: item.GetError(),
						logId: (long?)item["logid"] ?? 0,
						noOperation: item["noop"].ToBCBool(),
						recentChangesId: (long?)item["rcid"] ?? 0,
						removed: item["removed"].ToReadOnlyList<string>(),
						revisionId: (long?)item["revid"] ?? 0));
				}
			}

			return tags;
		}
		#endregion
	}
}