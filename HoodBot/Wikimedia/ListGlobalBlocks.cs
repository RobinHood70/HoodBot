namespace RobinHood70.HoodBot.Wikimedia
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Eve;
	using RobinHood70.WallE.Eve.Modules;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	public sealed class ListGlobalBlocks(WikiAbstractionLayer wal, GlobalBlocksInput input) : ListModule<GlobalBlocksInput, GlobalBlocksResult>(wal, input)
	{
		#region Constructors
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 113;

		public override string Name => "globalblocks";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "bg";
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, GlobalBlocksInput input)
		{
			input.ThrowNull();
			request
				.NotNull()
				.Add("start", input.Start)
				.Add("end", input.End)
				.AddIf("dir", "newer", input.SortAscending)
				.Add("ids", input.Ids)
				.Add("addresses", input.Addresses)
				.AddIfNotNull("ip", input.IP?.ToString())
				.AddFlags("prop", input.Properties)
				.Add("limit", this.Limit);
		}

		protected override GlobalBlocksResult? GetItem(JToken result) => result == null
			? null
			: new GlobalBlocksResult(
				address: (string?)result["address"],
				anononly: result["anononly"].GetBCBool(),
				by: (string?)result["by"],
				byWiki: (string?)result["byid"],
				expiry: result["expiry"].GetNullableDate(),
				id: (long?)result["id"] ?? 0,
				rangeStart: (string?)result["rangestart"],
				rangeEnd: (string?)result["rangeend"],
				reason: (string?)result["reason"],
				timestamp: result["timestamp"].GetNullableDate());
		#endregion
	}
}