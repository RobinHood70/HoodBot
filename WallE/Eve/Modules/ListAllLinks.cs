namespace RobinHood70.WallE.Eve.Modules;

using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Linq;
using RobinHood70.CommonCode;
using RobinHood70.WallE.Base;
using RobinHood70.WallE.Properties;
using RobinHood70.WikiCommon.Properties;
using RobinHood70.WikiCommon.RequestBuilder;

internal sealed class ListAllLinks : ListModule<IAllLinksInput, AllLinksItem>, IGeneratorModule
{
	#region Constructors
	public ListAllLinks(WikiAbstractionLayer wal, IAllLinksInput input)
		: this(wal, input, null)
	{
	}

	[SuppressMessage("Style", "IDE0072:Add missing cases", Justification = "Other cases are covered by preceding error message.")]
	public ListAllLinks(WikiAbstractionLayer wal, IAllLinksInput input, IPageSetGenerator? pageSetGenerator)
		: base(wal, input, pageSetGenerator)
	{
		if (!input.LinkType.IsUniqueFlag())
		{
			throw new InvalidOperationException(Globals.CurrentCulture(EveMessages.InputNonUnique, nameof(ListAllLinks), input.LinkType));
		}

		(this.Prefix, this.Name) = input.LinkType switch
		{
			AllLinksTypes.Links => ("al", "alllinks"),
			AllLinksTypes.FileUsages => ("af", "allfileusages"),
			AllLinksTypes.Redirects => ("ar", "allredirects"),
			AllLinksTypes.Transclusions => ("at", "alltransclusions"),
			_ => throw new InvalidOperationException(GlobalMessages.InvalidSwitchValue),
		};
	}
	#endregion

	#region Public Override Properties
	public override int MinimumVersion => 111;

	public override string Name { get; } // Set in constructor
	#endregion

	#region Protected Override Properties
	protected override string Prefix { get; }
	#endregion

	#region Public Static Methods
	public static ListAllLinks CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) =>
		new(wal, (IAllLinksInput)input, pageSetGenerator);
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestLocal(Request request, IAllLinksInput input)
	{
		ArgumentNullException.ThrowIfNull(request);
		ArgumentNullException.ThrowIfNull(input);
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

	protected override AllLinksItem? GetItem(JToken result) => result?.HasValues != true
		? null
		: new AllLinksItem(
			ns: (int?)result["ns"],
			title: (string?)result["title"],
			fromId: (long?)result["fromid"] ?? 0);
	#endregion
}