namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class ListTags : ListModule<TagsInput, TagsItem>
	{
		#region Constructors
		public ListTags(WikiAbstractionLayer wal, TagsInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 116;

		public override string Name => "tags";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "tg";
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, TagsInput input)
		{
			// At least up to 1.26, name is always included, so strip that off.
			var prop = input.NotNull(nameof(input)).Properties & ~TagProperties.Name;
			request
				.NotNull(nameof(request))
				.AddFlags("prop", prop)
				.Add("limit", this.Limit);
		}

		protected override TagsItem? GetItem(JToken result)
		{
			if (result == null)
			{
				return null;
			}

			// displayname can return false when message for tag is non-existent, blank, or "-", so check for that and convert to null.
			var displayToken = result["displayname"];
			var displayName = (displayToken == null || displayToken.Type != JTokenType.String) ? null : (string?)displayToken;

			return new TagsItem(
				name: result.MustHaveString("name"),
				description: (string?)result["description"],
				displayName: displayName,
				hitCount: (int?)result["hitcount"] ?? 0);
		}
		#endregion
	}
}