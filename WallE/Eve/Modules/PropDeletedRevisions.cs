#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WallE.Eve;
	using RobinHood70.WallE.Properties;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class PropDeletedRevisions : PropListModule<DeletedRevisionsInput, RevisionItem>, IGeneratorModule
	{
		#region Constructors
		public PropDeletedRevisions(WikiAbstractionLayer wal, DeletedRevisionsInput input)
			: this(wal, input, null)
		{
		}

		public PropDeletedRevisions(WikiAbstractionLayer wal, DeletedRevisionsInput input, IPageSetGenerator? pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 125;

		public override string Name { get; } = "deletedrevisions";
		#endregion

		#region Protected Override Properties
		protected override string Prefix { get; } = "drv";
		#endregion

		#region Public Static Methods
		public static PropDeletedRevisions CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) =>
			input is DeletedRevisionsInput propInput
				? new PropDeletedRevisions(wal, propInput, pageSetGenerator)
				: throw InvalidParameterType(nameof(input), nameof(DeletedRevisionsInput), input.GetType().Name);

		public static PropDeletedRevisions CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) =>
			input is DeletedRevisionsInput propInput
				? new PropDeletedRevisions(wal, propInput)
				: throw InvalidParameterType(nameof(input), nameof(DeletedRevisionsInput), input.GetType().Name);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, DeletedRevisionsInput input)
		{
			if (this.IsGenerator && this.SiteVersion < 125)
			{
				throw new WikiException(EveMessages.RevisionsGeneratorVersionInvalid);
			}

			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.BuildRevisions(input, this.SiteVersion)
				.AddIfNotNull("tag", input.Tag)
				.AddIf("limit", this.Limit, input.Limit > 0 || input.MaxItems > 1); // TODO: Needs testing when limits/maxitems are actually set to positive values. Limits are weird in this module, but since they're per-query, I believe this should work as written.
		}

		protected override RevisionItem GetItem(JToken result, PageItem page) => result.GetRevision();

		protected override ICollection<RevisionItem> GetMutableList(PageItem page) => (ICollection<RevisionItem>)page.DeletedRevisions;
		#endregion
	}
}
