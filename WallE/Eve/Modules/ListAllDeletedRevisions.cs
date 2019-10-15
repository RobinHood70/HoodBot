#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ListAllDeletedRevisions : ListModule<AllDeletedRevisionsInput, AllRevisionsItem>, IGeneratorModule
	{
		#region Constructors
		public ListAllDeletedRevisions(WikiAbstractionLayer wal, AllDeletedRevisionsInput input)
			: this(wal, input, null)
		{
		}

		public ListAllDeletedRevisions(WikiAbstractionLayer wal, AllDeletedRevisionsInput input, IPageSetGenerator pageSetGenerator)
			: base(wal, input, pageSetGenerator)
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
		public static ListAllDeletedRevisions CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new ListAllDeletedRevisions(wal, input as AllDeletedRevisionsInput, pageSetGenerator);
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

		protected override AllRevisionsItem? GetItem(JToken result)
		{
			if (result == null)
			{
				return null;
			}

			var title = result.StringNotNull("title");
			var revisions = result.GetRevisions(title);
			return new AllRevisionsItem(
				ns: (int)result.NotNull("ns"),
				title: title,
				pageId: (long)result.NotNull("pageid"),
				revisions: revisions);
		}
		#endregion
	}
}
