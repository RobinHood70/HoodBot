#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Properties;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ListAllLinks : ListModule<IAllLinksInput, AllLinksItem>, IGeneratorModule
	{
		#region Constructors
		public ListAllLinks(WikiAbstractionLayer wal, IAllLinksInput input)
			: this(wal, input, null)
		{
		}

		public ListAllLinks(WikiAbstractionLayer wal, IAllLinksInput input, IPageSetGenerator pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
			var linkType = input.LinkType;
			(this.Prefix, this.Name) = linkType switch
			{
				AllLinksTypes.Links => ("al", "alllinks"),
				AllLinksTypes.FileUsages => ("af", "allfileusages"),
				AllLinksTypes.Redirects => ("ar", "allredirects"),
				AllLinksTypes.Transclusions => ("at", "alltransclusions"),
				_ => throw new ArgumentException(CurrentCulture(linkType.IsUniqueFlag() ? EveMessages.ParameterInvalid : EveMessages.InputNonUnique, nameof(ListAllLinks), linkType)),
			};
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 111;

		public override string Name { get; }
		#endregion

		#region Protected Override Properties
		protected override string Prefix { get; }
		#endregion

		#region Public Static Methods
		public static ListAllLinks CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new ListAllLinks(wal, input as IAllLinksInput, pageSetGenerator);
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