#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ActionEdit : ActionModule<EditInput, EditResult>
	{
		#region Constructors
		public ActionEdit(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 113;

		public override string Name => "edit";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType => RequestType.Post;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, EditInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			if (input.Text?.Length > 8000)
			{
				request.Type = RequestType.PostMultipart;
			}

			var md5Text = (input.Text ?? (input.PrependText + input.AppendText)).GetHash(HashType.Md5);
			request
				.AddIfNotNull("title", input.Title)
				.AddIf("pageid", input.PageId, input.Title == null)
				.AddIf("section", input.Section, input.Section >= 0)
				.AddIf("section", "new", input.Section < 0)
				.AddIfNotNull("sectiontitle", input.SectionTitle)
				.AddIfNotNull("text", input.Text)
				.AddIfNotNull("summary", input.Summary)
				.Add("tags", input.Tags)
				.AddTristate("minor", "notminor", input.Minor)
				.Add("bot", input.Bot)
				.Add("basetimestamp", input.BaseTimestamp)
				.Add("starttimestamp", input.StartTimestamp)
				.Add("recreate", input.Recreate)
				.AddTristate("createonly", "nocreate", input.RequireNewPage)
				.AddIfPositive("watchlist", input.Watchlist)
				.AddIfNotNull("prependtext", input.PrependText)
				.AddIfNotNull("appendtext", input.AppendText)
				.AddIfPositive("undo", input.UndoRevision)
				.AddIfPositive("undoafter", input.UndoAfterRevision)
				.Add("redirect", input.Redirect)
				.AddIfNotNull("contentformat", input.ContentFormat)
				.AddIfNotNull("contentmodel", input.ContentModel)
				.AddIfNotNull("md5", md5Text)
				.AddHidden(input.CaptchaSolution)
				.AddHidden("token", input.Token);
		}

		protected override EditResult DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			return new EditResult(
				result: result.MustHaveString("result"),
				pageId: (long?)result.MustHave("pageid") ?? 0,
				title: result.MustHaveString("title"),
				flags: result.GetFlags(
					("new", EditFlags.New),
					("nochange", EditFlags.NoChange)),
				contentModel: (string?)result["contentmodel"],
				oldRevisionId: (long?)result["oldrevid"] ?? 0,
				newRevisionId: (long?)result["newrevid"] ?? 0,
				newTimestamp: (DateTime?)result["newtimestamp"],
				captchaData: result["captcha"].GetStringDictionary<string>());
		}
		#endregion
	}
}