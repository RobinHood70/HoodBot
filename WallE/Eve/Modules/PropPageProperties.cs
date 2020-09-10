﻿#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.CommonCode.Globals;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

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
		public static PropPageProperties CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) =>
			input is PagePropertiesInput propInput
				? new PropPageProperties(wal, propInput)
				: throw InvalidParameterType(nameof(input), nameof(PagePropertiesInput), input.GetType().Name);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, PagePropertiesInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request.Add("prop", input.Properties);
		}

		protected override void DeserializeToPage(JToken result, PageItem page)
		{
			ThrowNull(result, nameof(result));
			ThrowNull(page, nameof(page));
			if (page.Properties is Dictionary<string, string?> dictionary)
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