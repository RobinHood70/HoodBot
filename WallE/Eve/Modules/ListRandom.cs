﻿namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	// MWVERSION: 1.29
	internal sealed class ListRandom : ListModule<RandomInput, WikiTitleItem>, IGeneratorModule
	{
		#region Constructors
		public ListRandom(WikiAbstractionLayer wal, RandomInput input)
			: this(wal, input, null)
		{
		}

		public ListRandom(WikiAbstractionLayer wal, RandomInput input, IPageSetGenerator? pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 112;

		public override string Name => "random";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "rn";
		#endregion

		#region Public Static Methods
		public static ListRandom CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new(wal, (RandomInput)input, pageSetGenerator);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, RandomInput input)
		{
			input.ThrowNull();
			request
				.NotNull()
				.Add("namespace", input.Namespaces)
				.AddIf("filterredir", "redirects", input.FilterRedirects == Filter.Only && this.SiteVersion >= 126)
				.AddIf("filterredir", "all", input.FilterRedirects == Filter.Any && this.SiteVersion >= 126)
				.AddIf("redirect", input.FilterRedirects == Filter.Only, this.SiteVersion < 126)
				.Add("limit", this.Limit);
		}

		protected override WikiTitleItem GetItem(JToken result) => result.GetWikiTitle();

		/* This was a nice idea, but later versions remove the former 10/20 limit, which means we're now inadvertently requesting hundreds or thousands. Better to let the warning come through on older versions, so the user knows.
		protected override int GetNumericLimit()
		{
			var limit = base.GetNumericLimit();
			return limit > 10 ? -1 : limit;
		} */
		#endregion
	}
}
