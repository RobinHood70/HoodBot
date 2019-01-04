#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.RequestBuilder;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	internal class ListBlocks : ListModule<BlocksInput, BlocksResult>
	{
		#region Constructors
		public ListBlocks(WikiAbstractionLayer wal, BlocksInput input)
			: base(wal, input, null)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 112;

		public override string Name { get; } = "blocks";
		#endregion

		#region Protected Override Properties
		protected override string Prefix { get; } = "bk";
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, BlocksInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
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

		protected override BlocksResult GetItem(JToken result) => result == null
			? null
			: new BlocksResult()
			{
				Id = (long?)result["id"] ?? 0,
				User = (string)result["user"],
				UserId = (long?)result["userid"] ?? 0,
				By = (string)result["by"],
				ById = (long?)result["byid"] ?? 0,
				Timestamp = (DateTime?)result["timestamp"],
				Expiry = result["expiry"].AsDate(),
				Reason = (string)result["reason"],
				Automatic = result["automatic"].AsBCBool(),
				Flags =
					result.GetFlag("allowusertalk", BlockOptions.AllowUserTalk) |
					result.GetFlag("anononly", BlockOptions.AnonymousOnly) |
					result.GetFlag("autoblock", BlockOptions.AutoBlock) |
					result.GetFlag("hidden", BlockOptions.Hidden) |
					result.GetFlag("nocreate", BlockOptions.NoCreate) |
					result.GetFlag("noemail", BlockOptions.NoEmail),
				RangeStart = (string)result["rangestart"],
				RangeEnd = (string)result["rangeend"],
			};
		#endregion
	}
}