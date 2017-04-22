#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Base;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static RobinHood70.Globals;

	internal class ListAllRevisions : ListModule<AllRevisionsInput, AllRevisionsItem>, IGeneratorModule
	{
		#region Constructors
		public ListAllRevisions(WikiAbstractionLayer wal, AllRevisionsInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Protected Internal Override Properties
		public override int MinimumVersion { get; } = 125;

		public override string Name { get; } = "allrevisions";
		#endregion

		#region Public Properties
		protected override string BasePrefix { get; } = "arv";

		#endregion

		#region Public Static Methods
		public static ListAllRevisions CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input) => new ListAllRevisions(wal, input as AllRevisionsInput);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, AllRevisionsInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.BuildRevisions(input, this.SiteVersion)
				.Add("namespace", input.Namespaces)
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
