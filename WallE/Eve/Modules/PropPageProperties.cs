namespace RobinHood70.WallE.Eve.Modules;

using System;
using Newtonsoft.Json.Linq;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon.RequestBuilder;

internal sealed class PropPageProperties(WikiAbstractionLayer wal, PagePropertiesInput input) : PropListModule<PagePropertiesInput, PagePropertiesResult, PagePropertiesItem>(wal, input, null)
{
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
		ArgumentNullException.ThrowIfNull(request);
		ArgumentNullException.ThrowIfNull(input);
		request
			.Add("prop", input.Properties);
	}

	protected override PagePropertiesItem? GetItem(JToken result)
	{
		ArgumentNullException.ThrowIfNull(result);
		var prop = (JProperty)result;
		return new PagePropertiesItem(prop.Name, (string?)prop.Value ?? string.Empty);
	}

	protected override PagePropertiesResult GetNewList(JToken parent) => [];
	#endregion
}