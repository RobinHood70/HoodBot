#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Base;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static WikiCommon.Globals;

	internal class ListInterwikiBacklinks : ListModule<InterwikiBacklinksInput, InterwikiBacklinksItem>, IGeneratorModule
	{
		#region Constructors
		public ListInterwikiBacklinks(WikiAbstractionLayer wal, InterwikiBacklinksInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Protected Internal Override Properties
		public override int MinimumVersion { get; } = 117;

		public override string Name { get; } = "iwbacklinks";
		#endregion

		#region Public Override Properties
		protected override string Prefix { get; } = "iwbl";
		#endregion

		#region Public Override Methods
		protected override void BuildRequestLocal(Request request, InterwikiBacklinksInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddIfNotNull("prefix", input.Prefix)
				.AddIfNotNull("title", input.Title)
				.AddFlags("prop", input.Properties)
				.AddIf("dir", "descending", input.SortDescending)
				.Add("limit", this.Limit);
		}

		protected override InterwikiBacklinksItem GetItem(JToken result)
		{
			if (result == null)
			{
				return null;
			}

			var item = new InterwikiBacklinksItem();
			item.GetWikiTitle(result);
			item.IsRedirect = result["redirect"].AsBCBool();
			item.InterwikiPrefix = (string)result["iwprefix"];
			item.InterwikiTitle = (string)result["iwtitle"];

			return item;
		}
		#endregion
	}
}
