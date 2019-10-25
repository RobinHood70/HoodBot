#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class PropTranscludedIn : PropListModule<TranscludedInInput, TranscludedInItem>, IGeneratorModule
	{
		#region Constructors
		public PropTranscludedIn(WikiAbstractionLayer wal, TranscludedInInput input)
			: this(wal, input, null)
		{
		}

		public PropTranscludedIn(WikiAbstractionLayer wal, TranscludedInInput input, IPageSetGenerator? pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 124;

		public override string Name => "transcludedin";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "ti";
		#endregion

		#region Public Static Methods
		public static PropTranscludedIn CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) =>
			input is TranscludedInInput propInput
				? new PropTranscludedIn(wal, propInput, pageSetGenerator)
				: throw InvalidParameterType(nameof(input), nameof(TranscludedInInput), input.GetType().Name);

		public static PropTranscludedIn CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) =>
			input is TranscludedInInput propInput
				? new PropTranscludedIn(wal, propInput)
				: throw InvalidParameterType(nameof(input), nameof(TranscludedInInput), input.GetType().Name);
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

		protected override TranscludedInItem? GetItem(JToken result, PageItem page) => result == null
			? null
			: new TranscludedInItem(
				ns: (int?)result["ns"],
				title: (string?)result["title"],
				pageId: (long?)result["pageid"] ?? 0,
				redirect: result["redirect"].ToBCBool());

		protected override ICollection<TranscludedInItem> GetMutableList(PageItem page) => (ICollection<TranscludedInItem>)page.TranscludedIn;
		#endregion
	}
}