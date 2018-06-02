#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Base;
	using Design;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static WikiCommon.Globals;

	internal class ListProtectedTitles : ListModule<ProtectedTitlesInput, ProtectedTitlesItem>, IGeneratorModule
	{
		#region Constructors
		public ListProtectedTitles(WikiAbstractionLayer wal, ProtectedTitlesInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Protected Internal Override Properties
		public override int MinimumVersion { get; } = 115;

		public override string Name { get; } = "protectedtitles";
		#endregion

		#region Public Override Properties
		protected override string Prefix { get; } = "pt";
		#endregion

		#region Public Static Methods
		public static ListProtectedTitles CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input) => new ListProtectedTitles(wal, input as ProtectedTitlesInput);
		#endregion

		#region Public Override Methods
		protected override void BuildRequestLocal(Request request, ProtectedTitlesInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			var prop = FlagFilter
				.Check(this.SiteVersion, input.Properties)
				.FilterBefore(117, ProtectedTitlesProperties.UserId)
				.FilterBefore(116, ProtectedTitlesProperties.ParsedComment)
				.Value;
			request
				.Add("namespace", input.Namespaces)
				.Add("level", input.Levels)
				.AddIf("dir", "newer", input.SortAscending)
				.Add("start", input.Start)
				.Add("end", input.End)
				.AddFlags("prop", prop)
				.Add("limit", this.Limit);
		}

		protected override ProtectedTitlesItem GetItem(JToken result)
		{
			if (result == null)
			{
				return null;
			}

			var item = new ProtectedTitlesItem()
			{
				Namespace = (int?)result["ns"],
				Title = (string)result["title"],
				Timestamp = (DateTime?)result["timestamp"],
				User = (string)result["user"],
				UserId = (long?)result["userid"] ?? 0,
				Comment = (string)result["comment"],
				ParsedComment = (string)result["parsedcomment"],
				Expiry = result["expiry"].AsDate(),
				Level = (string)result["level"],
			};
			return item;
		}
		#endregion
	}
}
