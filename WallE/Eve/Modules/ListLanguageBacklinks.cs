#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Base;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static WikiCommon.Globals;

	internal class ListLanguageBacklinks : ListModule<LanguageBacklinksInput, LanguageBacklinksItem>, IGeneratorModule
	{
		#region Constructors
		public ListLanguageBacklinks(WikiAbstractionLayer wal, LanguageBacklinksInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Protected Internal Override Properties
		public override int MinimumVersion { get; } = 118;

		public override string Name { get; } = "langbacklinks";
		#endregion

		#region Public Override Properties
		protected override string Prefix { get; } = "lbl";
		#endregion

		#region Public Static Methods
		public static ListLanguageBacklinks CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input) => new ListLanguageBacklinks(wal, input as LanguageBacklinksInput);
		#endregion

		#region Public Override Methods
		protected override void BuildRequestLocal(Request request, LanguageBacklinksInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddIfNotNull("lang", input.Language)
				.AddIfNotNull("title", input.Title)
				.AddFlags("prop", input.Properties)
				.AddIf("dir", "descending", input.SortDescending)
				.Add("limit", this.Limit);
		}

		protected override LanguageBacklinksItem GetItem(JToken result)
		{
			if (result == null)
			{
				return null;
			}

			var item = new LanguageBacklinksItem();
			item.GetWikiTitle(result);
			item.IsRedirect = result["redirect"].AsBCBool();
			item.LanguageCode = (string)result["lllang"];
			item.LanguageTitle = (string)result["lltitle"];

			return item;
		}
		#endregion
	}
}
