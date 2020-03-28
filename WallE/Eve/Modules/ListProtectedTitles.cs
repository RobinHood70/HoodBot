#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.CommonCode.Globals;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal class ListProtectedTitles : ListModule<ProtectedTitlesInput, ProtectedTitlesItem>, IGeneratorModule
	{
		#region Constructors
		public ListProtectedTitles(WikiAbstractionLayer wal, ProtectedTitlesInput input)
			: this(wal, input, null)
		{
		}

		public ListProtectedTitles(WikiAbstractionLayer wal, ProtectedTitlesInput input, IPageSetGenerator? pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 115;

		public override string Name => "protectedtitles";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "pt";
		#endregion

		#region Public Static Methods
		public static ListProtectedTitles CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) =>
			input is ProtectedTitlesInput listInput
				? new ListProtectedTitles(wal, listInput, pageSetGenerator)
				: throw InvalidParameterType(nameof(input), nameof(ProtectedTitlesInput), input.GetType().Name);
		#endregion

		#region Protected Override Methods
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

		protected override ProtectedTitlesItem? GetItem(JToken result) => result == null
			? null
			: new ProtectedTitlesItem(
				ns: (int)result.MustHave("ns"),
				title: result.MustHaveString("title"),
				comment: (string?)result["comment"],
				expiry: result["expiry"].GetNullableDate(),
				level: (string?)result["level"],
				parsedComment: (string?)result["parsedcomment"],
				timestamp: (DateTime?)result["timestamp"],
				user: (string?)result["user"],
				userId: (long?)result["userid"] ?? 0);
		#endregion
	}
}
