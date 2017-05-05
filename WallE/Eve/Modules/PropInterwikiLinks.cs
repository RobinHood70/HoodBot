#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Base;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using WikiCommon;
	using static WikiCommon.Globals;

	internal class PropInterwikiLinks : PropListModule<InterwikiLinksInput, InterwikiTitleItem>
	{
		#region Constructors
		public PropInterwikiLinks(WikiAbstractionLayer wal, InterwikiLinksInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Protected Internal Override Properties
		public override int MinimumVersion { get; } = 117;

		public override string Name { get; } = "iwlinks";
		#endregion

		#region Public Override Properties
		protected override string BasePrefix { get; } = "iw";
		#endregion

		#region Public Static Methods
		public static PropInterwikiLinks CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new PropInterwikiLinks(wal, input as InterwikiLinksInput);
		#endregion

		#region Public Override Methods
		protected override void BuildRequestLocal(Request request, InterwikiLinksInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddIf("url", input.Properties.HasFlag(InterwikiLinksProperties.Url), this.SiteVersion < 124)
				.AddFlagsIf("prop", input.Properties, this.SiteVersion >= 124)
				.AddIfNotNull("prefix", input.Prefix)
				.AddIfNotNull("title", input.Title)
				.AddIf("dir", "descending", input.SortDescending)
				.Add("limit", this.Limit);
		}

		protected override InterwikiTitleItem GetItem(JToken result)
		{
			if (result == null)
			{
				return null;
			}

			var item = new InterwikiTitleItem()
			{
				InterwikiPrefix = (string)result["prefix"],
				Title = (string)result.AsBCContent("title"),
				Url = (Uri)result["url"],
			};
			return item;
		}

		protected override void GetResultsFromCurrentPage() => this.ResetMyList(this.Output.InterwikiLinks);

		protected override void SetResultsOnCurrentPage() => this.Output.InterwikiLinks = this.MyList.AsNewReadOnlyList();
		#endregion
	}
}
