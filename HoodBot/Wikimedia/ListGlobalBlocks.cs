namespace RobinHood70.HoodBot.Wikimedia;

using System;
using Newtonsoft.Json.Linq;
using RobinHood70.WallE.Eve;
using RobinHood70.WallE.Eve.Modules;
using RobinHood70.WikiCommon.RequestBuilder;
using static RobinHood70.WallE.Eve.ParsingExtensions;

public sealed class ListGlobalBlocks(WikiAbstractionLayer wal, GlobalBlocksInput input) : ListModule<GlobalBlocksInput, GlobalBlocksResult>(wal, input)
{
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
		ArgumentNullException.ThrowIfNull(input);
		ArgumentNullException.ThrowIfNull(request);
		var prop = input.Properties;
		if (this.SiteVersion < 144)
		{
			if (prop.HasFlag(GlobalBlocksProperties.Target))
			{
				prop = prop & ~GlobalBlocksProperties.Target | GlobalBlocksProperties.Address;
			}
		}
		else if (prop.HasFlag(GlobalBlocksProperties.Address))
		{
			prop = prop & ~GlobalBlocksProperties.Address | GlobalBlocksProperties.Target;
		}

		request
				.Add("start", input.Start)
				.Add("end", input.End)
				.AddIf("dir", "newer", input.SortAscending)
				.Add("ids", input.Ids)
				.Add("addresses", input.Addresses)
				.AddIfNotNull("ip", input.IP?.ToString())
				.AddFlags("prop", prop)
				.Add("limit", this.Limit);
	}

	protected override GlobalBlocksResult? GetItem(JToken result) => result == null
		? null
		: new GlobalBlocksResult(
			Address: (string?)result["target"] ?? (string?)result["address"],
			AnonymousOnly: result["anononly"].GetBCBool(),
			By: (string?)result["by"],
			ByWiki: (string?)result["byid"],
			Expiry: result["expiry"].GetNullableDate(),
			Id: (long?)result["id"] ?? 0,
			RangeStart: (string?)result["rangestart"],
			RangeEnd: (string?)result["rangeend"],
			Reason: (string?)result["reason"],
			Timestamp: result["timestamp"].GetNullableDate());
	#endregion
}