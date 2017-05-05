#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Base;
	using Design;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static WikiCommon.Globals;

	internal class ListUsers : ListModule<UsersInput, UsersItem>
	{
		#region Constructors
		public ListUsers(WikiAbstractionLayer wal, UsersInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Protected Internal Override Properties
		public override int MinimumVersion { get; } = 112;

		public override string Name { get; } = "users";
		#endregion

		#region Public Override Properties
		protected override string BasePrefix { get; } = "us";
		#endregion

		#region Public Override Methods
		protected override void BuildRequestLocal(Request request, UsersInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			var prop = FlagFilter
				.Check(this.SiteVersion, input.Properties)
				.FilterBefore(117, UsersProperties.Rights)
				.FilterBefore(118, UsersProperties.ImplicitGroups)
				.Value;
			request
				.Add("users", input.Users)
				.AddIf("token", TokensInput.UserRights, input.GetRightsToken)
				.AddFlags("prop", prop);
		}

		protected override UsersItem GetItem(JToken result)
		{
			if (result == null)
			{
				return null;
			}

			var item = new UsersItem();
			result.GetUser(item);
			item.Flags =
				result.GetFlag("emailable", UserFlags.Emailable) |
				result.GetFlag("interwiki", UserFlags.Interwiki) |
				result.GetFlag("invalid", UserFlags.Invalid) |
				result.GetFlag("missing", UserFlags.Missing);
			item.Token = (string)result["userrightstoken"];

			var genderNode = result["gender"];
			if (genderNode != null)
			{
				if (Enum.TryParse((string)genderNode, true, out Gender gender))
				{
					item.Gender = gender;
				}
			}

			return item;
		}
		#endregion
	}
}