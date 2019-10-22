#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using System.Text;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ActionImport : ActionModule<ImportInput, IReadOnlyList<ImportItem>>
	{
		#region Constructors
		public ActionImport(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 115;

		public override string Name { get; } = "import";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType { get; } = RequestType.Post;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, ImportInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			if (input.Xml != null)
			{
				request.Type = RequestType.PostMultipart;
			}

			request
				.AddIfNotNull("summary", input.Summary)
				.Add("xml", "dummyName", Encoding.ASCII.GetBytes(input.Xml))
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
					invalid: item["invalid"].ToBCBool()));
			}

			return output;
		}
		#endregion
	}
}
