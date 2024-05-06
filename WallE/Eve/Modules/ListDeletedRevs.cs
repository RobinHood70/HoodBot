namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class ListDeletedRevs : ListModule<ListDeletedRevisionsInput, DeletedRevisionsItem>
	{
		#region Constructors
		public ListDeletedRevs(WikiAbstractionLayer wal, ListDeletedRevisionsInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 112;

		public override string Name => "deletedrevs";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "dr";
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, ListDeletedRevisionsInput input)
		{
			ArgumentNullException.ThrowIfNull(request);
			ArgumentNullException.ThrowIfNull(input);
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
				.AddIfNotNullIf("tag", input.Tag, this.SiteVersion >= 118)
				.AddIfNotNull(input.ExcludeUser ? "excludeuser" : "user", input.User)
				.Add("namespace", input.Namespace)
				.AddFlags("prop", prop)
				.Add("limit", this.Limit);
		}

		protected override DeletedRevisionsItem? GetItem(JToken result) => result == null
			? null
			: new DeletedRevisionsItem(
			ns: (int)result.MustHave("ns"),
			title: result.MustHaveString("title"),
			pageId: (long)result.MustHave("pageid"),
			revisions: result.GetRevisions(),
			token: (string?)result["token"]);
		#endregion
	}
}