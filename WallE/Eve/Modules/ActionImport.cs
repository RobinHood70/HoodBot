#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.CommonCode.Globals;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal class ActionImport : ActionModule<ImportInput, IReadOnlyList<ImportItem>>
	{
		#region Constructors
		public ActionImport(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

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
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
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

		protected override IReadOnlyList<ImportItem> DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			var output = new List<ImportItem>();
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
}
