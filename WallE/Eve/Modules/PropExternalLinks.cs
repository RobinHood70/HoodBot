#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class PropExternalLinks : PropListModule<ExternalLinksInput, string>
	{
		#region Constructors
		public PropExternalLinks(WikiAbstractionLayer wal, ExternalLinksInput input)
			: base(wal, input, null)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 111;

		public override string Name => "extlinks";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "el";
		#endregion

		#region Public Static Methods
		public static PropExternalLinks CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) =>
			input is ExternalLinksInput propInput
				? new PropExternalLinks(wal, propInput)
				: throw InvalidParameterType(nameof(input), nameof(ExternalLinksInput), input.GetType().Name);
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

		protected override string? GetItem(JToken result, PageItem page) => result.MustHaveBCString("url");

		protected override ICollection<string> GetMutableList(PageItem page) => (ICollection<string>)page.ExternalLinks;
		#endregion
	}
}