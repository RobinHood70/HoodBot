#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Base;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static RobinHood70.Globals;

	internal class ListTags : ListModule<TagsInput, TagsItem>
	{
		#region Constructors
		public ListTags(WikiAbstractionLayer wal, TagsInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Protected Internal Override Properties
		public override int MinimumVersion { get; } = 116;

		public override string Name { get; } = "tags";
		#endregion

		#region Public Override Properties
		protected override string BasePrefix { get; } = "tg";
		#endregion

		#region Public Override Methods
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

		protected override TagsItem GetItem(JToken result)
		{
			if (result == null)
			{
				return null;
			}

			var item = new TagsItem()
			{
				Name = (string)result["name"],
				DisplayName = (string)result["displayname"],
				Description = (string)result["description"],
				HitCount = (int?)result["hitcount"] ?? 0,
			};
			return item;
		}
		#endregion
	}
}
