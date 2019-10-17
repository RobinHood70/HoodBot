#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ListAllRevisions : ListModule<AllRevisionsInput, AllRevisionsItem>, IGeneratorModule
	{
		#region Constructors
		public ListAllRevisions(WikiAbstractionLayer wal, AllRevisionsInput input)
			: this(wal, input, null)
		{
		}

		public ListAllRevisions(WikiAbstractionLayer wal, AllRevisionsInput input, IPageSetGenerator pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 125;

		public override string Name { get; } = "allrevisions";
		#endregion

		#region Protected Override Properties
		protected override string Prefix { get; } = "arv";
		#endregion

		#region Public Static Methods
		public static ListAllRevisions CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new ListAllRevisions(wal, input as AllRevisionsInput, pageSetGenerator);
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

		protected override AllRevisionsItem? GetItem(JToken result)
		{
			if (result == null)
			{
				return null;
			}

			var title = result.SafeString("title");
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
