#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	// For the time being, the complexity involved in implementing FileUsage, LinksHere, Redirects, and TranscludedIn as derived modules is just not worth it in C#. MediaWiki gets away with it much more easily due to the fact that it's mostly working with strings, along with PHP's looser type system. It may become more worthwhile to implement these modules as inherited in the future, however, or do it like the Revisions classes do and have a static base builder/deserializer but remain otherwise uninherited.
	internal class PropFileUsage : PropListModule<FileUsageInput, FileUsageItem>, IGeneratorModule
	{
		#region Constructors
		public PropFileUsage(WikiAbstractionLayer wal, FileUsageInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 124;

		public override string Name { get; } = "fileusage";
		#endregion

		#region Protected Override Properties
		protected override string Prefix { get; } = "fu";
		#endregion

		#region Public Static Methods
		public static PropFileUsage CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input) => new PropFileUsage(wal, input as FileUsageInput);

		public static PropFileUsage CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new PropFileUsage(wal, input as FileUsageInput);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, FileUsageInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddFlags("prop", input.Properties)
				.Add("namespace", input.Namespaces)
				.AddFilterPiped("show", "redirect", input.FilterRedirects)
				.Add("limit", this.Limit);
		}

		protected override FileUsageItem GetItem(JToken result) => result == null
			? null
			: new FileUsageItem
			{
				Redirect = result["redirect"].AsBCBool()
			}.GetWikiTitle(result);

		protected override void GetResultsFromCurrentPage() => this.ResetItems(this.Output.FileUsages);

		protected override void SetResultsOnCurrentPage() => this.Output.FileUsages = this.Items;
		#endregion
	}
}