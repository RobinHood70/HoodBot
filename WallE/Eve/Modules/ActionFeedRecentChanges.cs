#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ActionFeedRecentChanges : ActionModule<FeedRecentChangesInput, CustomResult>
	{
		#region Constructors
		public ActionFeedRecentChanges(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 123;

		public override string Name => "feedrecentchanges";
		#endregion

		#region Protected Override Properties
		protected override bool ForceCustomDeserialization => true;

		protected override RequestType RequestType => RequestType.Get;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, FeedRecentChangesInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddIfNotNull("feedformat", input.FeedFormat)
				.Add("namespace", input.Namespace)
				.Add("invert", input.Invert)
				.Add("associated", input.Associated)
				.AddIfPositive("days", input.Days)
				.AddIfPositive("limit", input.Limit > 50 ? 50 : input.Limit)
				.Add("from", input.From)
				.Add("hideminor", input.HideMinor)
				.Add("hidebots", input.HideBots)
				.Add("hideanons", input.HideAnonymous)
				.Add("hideliu", input.HideLoggedInUsers)
				.Add("hidepatrolled", input.HidePatrolled)
				.Add("hidemyself", input.HideMyself)
				.Add("hidecategorization", input.HideCategorization)
				.Add("tagfilter", input.TagFilter)
				.Add("target", input.Target)
				.Add("showlinkedto", input.ShowLinkedTo)
				.Add("categories", input.Categories)
				.Add("categories_any", input.CategoriesAny);
		}

		protected override CustomResult DeserializeCustom(string? result) => new CustomResult(result);

		protected override CustomResult DeserializeResult(JToken result) => throw new NotSupportedException();
		#endregion
	}
}
