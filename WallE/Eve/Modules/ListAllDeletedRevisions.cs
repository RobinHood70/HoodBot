#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ListAllDeletedRevisions : ListModule<AllDeletedRevisionsInput, AllRevisionsItem>, IGeneratorModule
	{
		#region Constructors
		public ListAllDeletedRevisions(WikiAbstractionLayer wal, AllDeletedRevisionsInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 125;

		public override string Name { get; } = "alldeletedrevisions";
		#endregion

		#region Protected Override Properties
		protected override string Prefix { get; } = "adr";
		#endregion

		#region Public Static Methods
		public static ListAllDeletedRevisions CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input) => new ListAllDeletedRevisions(wal, input as AllDeletedRevisionsInput);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, AllDeletedRevisionsInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.BuildRevisions(input, this.SiteVersion)
				.Add("namespace", input.Namespaces)
				.AddIfNotNull("from", input.From)
				.AddIfNotNull("to", input.To)
				.AddIfNotNull("prefix", input.Prefix)
				.AddIfNotNull("tag", input.Tag)
				.Add("generatetitles", input.GenerateTitles);
		}

		protected override AllRevisionsItem GetItem(JToken result)
		{
			if (result == null)
			{
				return null;
			}

			var item = new AllRevisionsItem();
			item.GetWikiTitle(result);

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
