namespace RobinHood70.WallE.Eve.Modules;

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon.RequestBuilder;
using static RobinHood70.WallE.Eve.ParsingExtensions;

internal sealed class ActionImport(WikiAbstractionLayer wal) : ActionModule<ImportInput, IReadOnlyList<ImportItem>>(wal)
{
	#region Public Override Properties
	public override int MinimumVersion => 115;

	public override string Name => "import";
	#endregion

	#region Protected Override Properties
	protected override RequestType RequestType => RequestType.Post;
	#endregion

	#region Protected Override Methods
	protected override void BuildRequestLocal(Request request, ImportInput input)
	{
		ArgumentNullException.ThrowIfNull(request);
		ArgumentNullException.ThrowIfNull(input);
		if (input.GetXmlData() is byte[] xmlData)
		{
			request.Type = RequestType.PostMultipart;
			request.Add("xml", "dummyName", xmlData);
		}

		request
			.AddIfNotNull("summary", input.Summary)
			.AddIfNotNull("interwikisource", input.InterwikiSource)
			.AddIfNotNull("interwikipage", input.InterwikiPage)
			.Add("fullhistory", input.FullHistory)
			.Add("template", input.Templates)
			.Add("namespace", input.Namespace)
			.AddIfNotNullIf("rootpage", input.RootPage, this.SiteVersion >= 120)
			.AddHidden("token", input.Token);
	}

	protected override IReadOnlyList<ImportItem> DeserializeResult(JToken? result)
	{
		ArgumentNullException.ThrowIfNull(result);
		List<ImportItem> output = [];
		foreach (var item in result)
		{
			output.Add(new ImportItem(
				ns: (int)result.MustHave("ns"),
				title: result.MustHaveString("title"),
				revisions: (int?)item["revisions"] ?? 0,
				invalid: item["invalid"].GetBCBool()));
		}

		return output;
	}
	#endregion
}