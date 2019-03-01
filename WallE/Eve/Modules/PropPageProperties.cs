#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class PropPageProperties : PropModule<PagePropertiesInput>
	{
		#region Constructors
		public PropPageProperties(WikiAbstractionLayer wal, PagePropertiesInput input)
			: base(wal, input, null)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 117;

		public override string Name { get; } = "pageprops";
		#endregion

		#region Protected Override Properties
		protected override string Prefix { get; } = "pp";
		#endregion

		#region Public Static Methods
		public static PropPageProperties CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new PropPageProperties(wal, input as PagePropertiesInput);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, PagePropertiesInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request.Add("prop", input.Properties);
		}

		protected override void DeserializeResult(JToken result, PageItem output)
		{
			ThrowNull(result, nameof(result));
			ThrowNull(output, nameof(output));
			output.Properties = result.AsReadOnlyDictionary<string, string>();
		}
		#endregion
	}
}