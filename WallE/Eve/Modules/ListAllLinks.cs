#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Properties;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.CommonCode.Globals;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class ListAllLinks : ListModule<IAllLinksInput, AllLinksItem>, IGeneratorModule
	{
		#region Fields
		private readonly string prefix;
		private readonly string name;
		#endregion

		#region Constructors
		public ListAllLinks(WikiAbstractionLayer wal, IAllLinksInput input)
			: this(wal, input, null)
		{
		}

		public ListAllLinks(WikiAbstractionLayer wal, IAllLinksInput input, IPageSetGenerator? pageSetGenerator)
			: base(wal, input, pageSetGenerator) => (this.prefix, this.name) = input.LinkType switch
			{
				AllLinksTypes.Links => ("al", "alllinks"),
				AllLinksTypes.FileUsages => ("af", "allfileusages"),
				AllLinksTypes.Redirects => ("ar", "allredirects"),
				AllLinksTypes.Transclusions => ("at", "alltransclusions"),
				_ => throw new InvalidOperationException(CurrentCulture(input.LinkType.IsUniqueFlag() ? EveMessages.ParameterInvalid : EveMessages.InputNonUnique, nameof(ListAllLinks), input.LinkType)),
			};
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 111;

		public override string Name => this.name;
		#endregion

		#region Protected Override Properties
		protected override string Prefix => this.prefix;
		#endregion

		#region Public Static Methods
		public static ListAllLinks CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) =>
			input is IAllLinksInput listInput
				? new ListAllLinks(wal, listInput, pageSetGenerator)
				: throw InvalidParameterType(nameof(input), nameof(IAllLinksInput), input.GetType().Name);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, IAllLinksInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			var prop = input.LinkType == AllLinksTypes.Redirects ?
				input.Properties :
				input.Properties & (AllLinksProperties.Ids | AllLinksProperties.Title);
			request
				.AddIfNotNull("from", input.From)
				.AddIfNotNull("to", input.To)
				.AddIf("namespace", input.Namespace, input.LinkType != AllLinksTypes.FileUsages)
				.AddIfNotNull("prefix", input.Prefix)
				.Add("unique", input.Unique)
				.AddIf("dir", "descending", input.SortDescending)
				.AddFlags("prop", prop)
				.Add("limit", this.Limit);
		}

		protected override AllLinksItem? GetItem(JToken result) => result == null || !result.HasValues
			? null
			: new AllLinksItem(
				ns: (int?)result["ns"],
				title: (string?)result["title"],
				fromId: (long?)result["fromid"] ?? 0);
		#endregion
	}
}