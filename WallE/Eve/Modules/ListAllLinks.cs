#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.RequestBuilder;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WallE.Properties.EveMessages;
	using static RobinHood70.WikiCommon.Globals;

	internal class ListAllLinks : ListModule<IAllLinksInput, AllLinksItem>, IGeneratorModule
	{
		#region Constructors
		public ListAllLinks(WikiAbstractionLayer wal, IAllLinksInput input)
			: base(wal, input)
		{
			switch (input.LinkType)
			{
				case AllLinksTypes.Links:
					this.Prefix = "al";
					this.Name = "alllinks";
					break;
				case AllLinksTypes.FileUsages:
					this.Prefix = "af";
					this.Name = "allfileusages";
					break;
				case AllLinksTypes.Redirects:
					this.Prefix = "ar";
					this.Name = "allredirects";
					break;
				case AllLinksTypes.Transclusions:
					this.Prefix = "at";
					this.Name = "alltransclusions";
					break;
				default:
					throw new ArgumentException(CurrentCulture(input.LinkType.IsUniqueFlag() ? ParameterInvalid : InputNonUnique, nameof(ListAllLinks), input.LinkType));
			}
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
		public static ListAllLinks CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input) => new ListAllLinks(wal, input as IAllLinksInput);
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

		protected override AllLinksItem GetItem(JToken result)
		{
			if (result == null || !result.HasValues)
			{
				return null;
			}

			var item = new AllLinksItem()
			{
				FromId = (long?)result["fromid"] ?? 0,
				Namespace = (int?)result["ns"],
				Title = (string)result["title"],
			};
			return item;
		}
		#endregion
	}
}