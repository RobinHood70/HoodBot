#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ListTags : ListModule<TagsInput, TagsItem>
	{
		#region Constructors
		public ListTags(WikiAbstractionLayer wal, TagsInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 116;

		public override string Name { get; } = "tags";
		#endregion

		#region Protected Override Properties
		protected override string Prefix { get; } = "tg";
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, TagsInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));

			// At least up to 1.26, name is always included, so strip that off.
			var prop = input.Properties & ~TagProperties.Name;
			request
				.AddFlags("prop", prop)
				.Add("limit", this.Limit);
		}

		protected override TagsItem? GetItem(JToken result) => result == null
			? null
			: new TagsItem(
				name: result.MustHaveString("name"),
				description: (string?)result["description"],
				displayName: (string?)result["displayname"],
				hitCount: (int?)result["hitcount"] ?? 0);
		#endregion
	}
}