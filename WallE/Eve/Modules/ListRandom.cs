#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Base;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static RobinHood70.Globals;

	// MWVERSION 1.29 - note that filterredir=all is simulated below 1.26 and will not produce the same distribution of redirects and non-redirects.
	internal class ListRandom : ListModule<RandomInput, WikiTitleItem>, IGeneratorModule
	{
		#region Constructors
		public ListRandom(WikiAbstractionLayer wal, RandomInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Protected Internal Override Properties
		public override int MinimumVersion { get; } = 112;

		public override string Name { get; } = "random";
		#endregion

		#region Public Override Properties
		protected override string BasePrefix { get; } = "rn";
		#endregion

		#region Public Static Methods
		public static ListRandom CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input) => new ListRandom(wal, input as RandomInput);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, RandomInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.Add("namespace", input.Namespaces)
				.AddIf("filterredir", "redirects", input.FilterRedirects == FilterOption.Only && this.SiteVersion >= 126)
				.AddIf("filterredir", "all", input.FilterRedirects == FilterOption.All && this.SiteVersion >= 126)
				.AddIf("redirect", input.FilterRedirects == FilterOption.Only || (input.FilterRedirects == FilterOption.All && new Random().NextDouble() < 0.5), this.SiteVersion < 126)
				.Add("limit", this.Limit);
		}

		protected override WikiTitleItem GetItem(JToken result) => result.GetWikiTitle();

		protected override int GetNumericLimit()
		{
			var limit = base.GetNumericLimit();
			return limit > 10 ? -1 : limit;
		}
		#endregion
	}
}
