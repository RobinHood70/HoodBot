#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.RequestBuilder;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	// MWVERSION 1.29 - note that filterredir=all is simulated below 1.26 and will not produce the same distribution of redirects and non-redirects.
	internal class ListRandom : ListModule<RandomInput, WikiTitleItem>, IGeneratorModule
	{
		#region Constructors
		public ListRandom(WikiAbstractionLayer wal, RandomInput input)
			: this(wal, input, null)
		{
		}

		public ListRandom(WikiAbstractionLayer wal, RandomInput input, IPageSetGenerator pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 112;

		public override string Name { get; } = "random";
		#endregion

		#region Protected Override Properties
		protected override string Prefix { get; } = "rn";
		#endregion

		#region Public Static Methods
		public static ListRandom CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new ListRandom(wal, input as RandomInput, pageSetGenerator);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, RandomInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.Add("namespace", input.Namespaces)
				.AddIf("filterredir", "redirects", input.FilterRedirects == Filter.Only && this.SiteVersion >= 126)
				.AddIf("filterredir", "all", input.FilterRedirects == Filter.Any && this.SiteVersion >= 126)
				.AddIf("redirect", input.FilterRedirects == Filter.Only || (input.FilterRedirects == Filter.Any && new Random().NextDouble() < 0.5), this.SiteVersion < 126)
				.Add("limit", this.Limit);
		}

		protected override WikiTitleItem GetItem(JToken result)
		{
			var title = result.GetWikiTitle();
			title.PageId = (long?)result["id"] ?? 0;
			return title;
		}

		protected override int GetNumericLimit()
		{
			var limit = base.GetNumericLimit();
			return limit > 10 ? -1 : limit;
		}
		#endregion
	}
}
