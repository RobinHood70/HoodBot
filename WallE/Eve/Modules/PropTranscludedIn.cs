#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class PropTranscludedIn : PropListModule<TranscludedInInput, TranscludedInItem>, IGeneratorModule
	{
		#region Constructors
		public PropTranscludedIn(WikiAbstractionLayer wal, TranscludedInInput input)
			: this(wal, input, null)
		{
		}

		public PropTranscludedIn(WikiAbstractionLayer wal, TranscludedInInput input, IPageSetGenerator pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 124;

		public override string Name { get; } = "transcludedin";
		#endregion

		#region Protected Override Properties
		protected override string Prefix { get; } = "ti";
		#endregion

		#region Public Static Methods
		public static PropTranscludedIn CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new PropTranscludedIn(wal, input as TranscludedInInput, pageSetGenerator);

		public static PropTranscludedIn CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new PropTranscludedIn(wal, input as TranscludedInInput);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, TranscludedInInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddFlags("prop", input.Properties)
				.Add("namespace", input.Namespaces)
				.AddFilterPiped("show", "redirect", input.FilterRedirects)
				.Add("limit", this.Limit);
		}

		protected override TranscludedInItem GetItem(JToken result) => result == null
			? null
			: new TranscludedInItem
			{
				Redirect = result["redirect"].AsBCBool()
			}.GetWikiTitle(result);

		protected override void GetResultsFromCurrentPage() => this.ResetItems(this.Output.TranscludedIn);

		protected override void SetResultsOnCurrentPage() => this.Output.TranscludedIn = this.CopyList();
		#endregion
	}
}