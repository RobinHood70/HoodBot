#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Base;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static RobinHood70.Globals;

	internal class ListExtUrlUsage : ListModule<ExternalUrlUsageInput, ExternalUrlUsageItem>, IGeneratorModule
	{
		#region Constructors
		public ListExtUrlUsage(WikiAbstractionLayer wal, ExternalUrlUsageInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Protected Internal Override Properties
		public override int MinimumVersion { get; } = 111;

		public override string Name { get; } = "exturlusage";
		#endregion

		#region Public Override Properties
		protected override string BasePrefix { get; } = "eu";
		#endregion

		#region Public Static Methods
		public static ListExtUrlUsage CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input) => new ListExtUrlUsage(wal, input as ExternalUrlUsageInput);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, ExternalUrlUsageInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddFlags("prop", input.Properties)
				.AddIfNotNull("protocol", input.Protocol)
				.AddIfNotNull("query", input.Query)
				.Add("namespace", input.Namespaces)
				.Add("expandurl", input.ExpandUrl)
				.Add("limit", this.Limit);
		}

		protected override ExternalUrlUsageItem GetItem(JToken result)
		{
			if (result == null)
			{
				return null;
			}

			var item = new ExternalUrlUsageItem();
			item.GetWikiTitle(result);
			item.Url = (string)result["url"];

			return item;
		}
		#endregion
	}
}