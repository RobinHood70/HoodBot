namespace RobinHood70.WallE.Eve.Modules;

using System;
using Newtonsoft.Json.Linq;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.RequestBuilder;
using static RobinHood70.WallE.Eve.ParsingExtensions;

internal sealed class ListBlocks(WikiAbstractionLayer wal, BlocksInput input) : ListModule<BlocksInput, BlocksResult>(wal, input)
{
	#region Public Override Properties
	public override int MinimumVersion => 112;

	public override string Name => "blocks";
	#endregion

	#region Protected Override Properties
	protected override string Prefix => "bk";
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestLocal(Request request, BlocksInput input)
	{
		ArgumentNullException.ThrowIfNull(input);
		ArgumentNullException.ThrowIfNull(request);
		request
			.Add("start", input.Start)
			.Add("end", input.End)
			.AddIf("dir", "newer", input.SortAscending)
			.Add("ids", input.Ids)
			.Add("users", input.Users)
			.AddIfNotNull("ip", input.IP?.ToString())
			.AddFlags("prop", input.Properties)
			.AddFilterPiped("show", "account", input.FilterAccount)
			.AddFilterPiped("show", "ip", input.FilterIP)
			.AddFilterPiped("show", "range", input.FilterRange)
			.AddFilterPiped("show", "temp", input.FilterTemporary)
			.Add("limit", this.Limit);
	}

	protected override BlocksResult? GetItem(JToken result) => result == null
		? null
		: new BlocksResult(
			automatic: result["automatic"].GetBCBool(),
			by: (string?)result["by"],
			byId: (long?)result["byid"] ?? 0,
			expiry: result["expiry"].GetNullableDate(),
			flags: result.GetFlags(
				("allowusertalk", BlockFlags.AllowUserTalk),
				("anononly", BlockFlags.AnonymousOnly),
				("autoblock", BlockFlags.AutoBlock),
				("hidden", BlockFlags.Hidden),
				("nocreate", BlockFlags.NoCreate),
				("noemail", BlockFlags.NoEmail)),
			id: (long?)result["id"] ?? 0,
			rangeStart: (string?)result["rangestart"],
			rangeEnd: (string?)result["rangeend"],
			reason: (string?)result["reason"],
			timestamp: result["timestamp"].GetNullableDate(),
			user: (string?)result["user"],
			userId: (long?)result["userid"] ?? 0);
	#endregion
}