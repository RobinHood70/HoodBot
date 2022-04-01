namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class ListUsers : ListModule<UsersInput, UsersItem>
	{
		#region Constructors
		public ListUsers(WikiAbstractionLayer wal, UsersInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 112;

		public override string Name => "users";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "us";
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, UsersInput input)
		{
			var prop = FlagFilter
				.Check(this.SiteVersion, input.NotNull().Properties)
				.FilterBefore(117, UsersProperties.Rights)
				.FilterBefore(118, UsersProperties.ImplicitGroups)
				.Value;
			request
				.NotNull()
				.Add("users", input.Users)
				.AddIf("token", TokensInput.UserRights, input.GetRightsToken)
				.AddFlags("prop", prop);
		}

		protected override UsersItem? GetItem(JToken result) => result == null
			? null
			: new UsersItem(
				baseUser: result.GetUser(),
				flags: result.GetFlags(
					("emailable", UserFlags.Emailable),
					("interwiki", UserFlags.Interwiki),
					("invalid", UserFlags.Invalid),
					("missing", UserFlags.Missing)),
				gender: (string?)result["gender"],
				token: (string?)result["userrightstoken"]);
		#endregion
	}
}