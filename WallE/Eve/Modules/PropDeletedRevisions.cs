#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Base;
	using Design;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using WikiCommon;
	using static Properties.EveMessages;
	using static WikiCommon.Globals;

	internal class PropDeletedRevisions : PropListModule<DeletedRevisionsInput, RevisionsItem>, IGeneratorModule
	{
		#region Constructors
		public PropDeletedRevisions(WikiAbstractionLayer wal, DeletedRevisionsInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Protected Internal Override Properties
		public override int MinimumVersion { get; } = 125;

		public override string Name { get; } = "deletedrevisions";
		#endregion

		#region Public Override Properties
		protected override string BasePrefix { get; } = "drv";
		#endregion

		#region Public Static Methods
		public static PropDeletedRevisions CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input) => new PropDeletedRevisions(wal, input as DeletedRevisionsInput);

		public static PropDeletedRevisions CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new PropDeletedRevisions(wal, input as DeletedRevisionsInput);
		#endregion

		#region Public Override Methods
		protected override void BuildRequestLocal(Request request, DeletedRevisionsInput input)
		{
			if (this.IsGenerator && this.SiteVersion < 125)
			{
				throw new WikiException(RevisionsGeneratorVersionInvalid);
			}

			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.BuildRevisions(input, this.SiteVersion)
				.AddIfNotNull("tag", input.Tag)
				.AddIf("limit", this.Limit, input.Limit > 0 || input.MaxItems > 1); // TODO: Needs testing when limits/maxitems are actually set to positive values. Limits are weird in this module, but since they're per-query, I believe this should work as written.
		}

		protected override RevisionsItem GetItem(JToken result) => result.GetRevision(this.Output.Title);

		protected override void GetResultsFromCurrentPage() => this.ResetMyList(this.Output.DeletedRevisions);

		protected override void SetResultsOnCurrentPage() => this.Output.DeletedRevisions = this.MyList.AsNewReadOnlyList();
		#endregion
	}
}
