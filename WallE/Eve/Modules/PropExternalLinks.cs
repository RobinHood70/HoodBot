#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class PropExternalLinks : PropListModule<ExternalLinksInput, string>
	{
		#region Constructors
		public PropExternalLinks(WikiAbstractionLayer wal, ExternalLinksInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 111;

		public override string Name { get; } = "extlinks";
		#endregion

		#region Protected Override Properties
		protected override string Prefix { get; } = "el";
		#endregion

		#region Public Static Methods
		public static PropExternalLinks CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new PropExternalLinks(wal, input as ExternalLinksInput);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, ExternalLinksInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.Add("expandurl", input.ExpandUrl)
				.AddIfNotNull("protocol", input.Protocol)
				.AddIfNotNull("query", input.Query)
				.Add("limit", this.Limit);
		}

		protected override string GetItem(JToken result) => (string)result.AsBCContent("url");

		protected override void GetResultsFromCurrentPage() => this.ResetItems(this.Output.ExternalLinks);

		protected override void SetResultsOnCurrentPage() => this.Output.ExternalLinks = this.Result();
		#endregion
	}
}