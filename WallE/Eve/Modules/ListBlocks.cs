#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Base;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static RobinHood70.Globals;

	internal class ListBlocks : ListModule<BlocksInput, BlocksResult>
	{
		#region Constructors
		public ListBlocks(WikiAbstractionLayer wal, BlocksInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Protected Internal Override Properties
		public override int MinimumVersion { get; } = 112;

		public override string Name { get; } = "blocks";
		#endregion

		#region Public Override Properties
		protected override string BasePrefix { get; } = "bk";

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
				.AddFilterOptionPiped("show", "account", input.FilterAccount)
				.AddFilterOptionPiped("show", "ip", input.FilterIP)
				.AddFilterOptionPiped("show", "range", input.FilterRange)
				.AddFilterOptionPiped("show", "temp", input.FilterTemporary)
				.Add("limit", this.Limit);
		}

		protected override BlocksResult GetItem(JToken result)
		{
			if (result == null)
			{
				return null;
			}

			var item = new BlocksResult()
			{
				Id = (long?)result["id"] ?? 0,
				User = (string)result["user"],
				UserId = (long?)result["userid"] ?? 0,
				By = (string)result["by"],
				ById = (long?)result["byid"] ?? 0,
				Timestamp = (DateTime?)result["timestamp"],
				Expiry = result["expiry"].AsDate(),
				Reason = (string)result["reason"],
				Flags =
				result.GetFlag("allowusertalk", BlockFlags.AllowUserTalk) |
				result.GetFlag("anononly", BlockFlags.AnonymousOnly) |
				result.GetFlag("autoblock", BlockFlags.AutoBlock) |
				result.GetFlag("automatic", BlockFlags.Automatic) |
				result.GetFlag("hidden", BlockFlags.Hidden) |
				result.GetFlag("nocreate", BlockFlags.NoCreate) |
				result.GetFlag("noemail", BlockFlags.NoEmail),
				RangeStart = (string)result["rangestart"],
				RangeEnd = (string)result["rangeend"],
			};
			return item;
		}
		#endregion
	}
}