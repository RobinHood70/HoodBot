namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;

	internal sealed class PropPageProperties : PropModule<PagePropertiesInput>
	{
		#region Constructors
		public PropPageProperties(WikiAbstractionLayer wal, PagePropertiesInput input)
			: base(wal, input, null)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 117;

		public override string Name => "pageprops";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "pp";
		#endregion

		#region Public Static Methods
		public static PropPageProperties CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new(wal, (PagePropertiesInput)input);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, PagePropertiesInput input)
		{
			input.ThrowNull();
			request
				.NotNull().Add("prop", input.Properties);
		}

		protected override void DeserializeToPage(JToken result, PageItem page)
		{
			result.ThrowNull();
			if (page.NotNull().Properties is Dictionary<string, string?> dictionary)
			{
				dictionary.Clear();
				foreach (var item in result.Children<JProperty>())
				{
					dictionary.Add(item.Name, (string?)item.Value);
				}
			}
		}
		#endregion
	}
}