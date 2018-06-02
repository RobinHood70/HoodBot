#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Base;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using WikiCommon;
	using static WikiCommon.Globals;

	internal class PropTranscludedIn : PropListModule<TranscludedInInput, TranscludedInItem>, IGeneratorModule
	{
		#region Constructors
		public PropTranscludedIn(WikiAbstractionLayer wal, TranscludedInInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Protected Internal Override Properties
		public override int MinimumVersion { get; } = 124;

		public override string Name { get; } = "transcludedin";
		#endregion

		#region Public Override Properties
		protected override string Prefix { get; } = "ti";
		#endregion

		#region Public Static Methods
		public static PropTranscludedIn CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input) => new PropTranscludedIn(wal, input as TranscludedInInput);

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

		protected override TranscludedInItem GetItem(JToken result)
		{
			if (result == null)
			{
				return null;
			}

			var item = new TranscludedInItem();
			item.GetWikiTitle(result);
			item.Redirect = result["redirect"].AsBCBool();

			return item;
		}

		protected override void GetResultsFromCurrentPage() => this.ResetMyList(this.Output.TranscludedIn);

		protected override void SetResultsOnCurrentPage() => this.Output.TranscludedIn = this.MyList.AsNewReadOnlyList();
		#endregion
	}
}