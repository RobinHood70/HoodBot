#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ListAllUsers : ListModule<AllUsersInput, AllUsersItem>
	{
		#region Constructors
		public ListAllUsers(WikiAbstractionLayer wal, AllUsersInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 111;

		public override string Name { get; } = "allusers";
		#endregion

		#region Protected Override Properties
		protected override string Prefix { get; } = "au";
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, AllUsersInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddIfNotNull("from", input.From)
				.AddIfNotNull("to", input.To)
				.AddIfNotNull("prefix", input.Prefix)
				.AddIf("dir", "descending", input.SortDescending)
				.Add(input.ExcludeGroups ? "excludegroup" : "group", input.Groups)
				.Add("rights", input.Rights)
				.AddFlags("prop", input.Properties)
				.Add("witheditsonly", input.WithEditsOnly)
				.Add("activeusers", input.ActiveUsersOnly)
				.Add("limit", this.Limit);
		}

		protected override AllUsersItem GetItem(JToken result)
		{
			if (result == null)
			{
				return null;
			}

			var item = new AllUsersItem
			{
				RecentActions = (int?)result["recentactions"] ?? (int?)result["recenteditcount"] ?? 0
			};
			result.GetUser(item);

			return item;
		}
		#endregion
	}
}