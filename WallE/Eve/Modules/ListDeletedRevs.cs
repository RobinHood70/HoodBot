#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Base;
	using Design;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static WikiCommon.Globals;

	internal class ListDeletedRevs : ListModule<ListDeletedRevisionsInput, DeletedRevisionsItem>
	{
		#region Constructors
		public ListDeletedRevs(WikiAbstractionLayer wal, ListDeletedRevisionsInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Protected Internal Override Properties
		public override int MinimumVersion { get; } = 112;

		public override string Name { get; } = "deletedrevs";
		#endregion

		#region Public Override Properties
		protected override string Prefix { get; } = "dr";
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, ListDeletedRevisionsInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			var prop = FlagFilter
				.Check(this.SiteVersion, input.Properties)
				.FilterBefore(123, DeletedRevisionsProperties.Tags)
				.FilterBefore(119, DeletedRevisionsProperties.Sha1)
				.FilterBefore(118, DeletedRevisionsProperties.ParentId)
				.FilterBefore(117, DeletedRevisionsProperties.UserId | DeletedRevisionsProperties.Tags)
				.FilterBefore(116, DeletedRevisionsProperties.ParsedComment)
				.Value;
			request
				.Add("start", input.Start)
				.Add("end", input.End)
				.AddIf("dir", "newer", input.SortAscending)
				.AddIfNotNull("from", input.From)
				.AddIfNotNullIf("to", input.To, this.SiteVersion >= 118)
				.AddIfNotNullIf("prefix", input.Prefix, this.SiteVersion >= 118)
				.Add("unique", input.Unique)
				.AddIf("tag", input.Tag, input.Tag != null && this.SiteVersion >= 118)
				.AddIfNotNull(input.ExcludeUser ? "excludeuser" : "user", input.User)
				.Add("namespace", input.Namespace)
				.AddFlags("prop", prop)
				.Add("limit", this.Limit);
		}

		protected override DeletedRevisionsItem GetItem(JToken result)
		{
			if (result == null)
			{
				return null;
			}

			var item = new DeletedRevisionsItem();
			item.GetWikiTitle(result);
			item.DeletedRevisionsToken = (string)result["token"];

			var revisions = new List<RevisionsItem>();
			foreach (var revisionNode in result["revisions"])
			{
				var revision = revisionNode.GetRevision(item.Title);
				revisions.Add(revision);
			}

			item.Revisions = revisions;

			return item;
		}
		#endregion
	}
}
