#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.ProjectGlobals;
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
		public override int MinimumVersion { get; } = 113;

		public override string Name { get; } = "edit";
		#endregion

		#region Internal Properties
		internal IReadOnlyDictionary<string, string> CaptchaData { get; private set; } = EmptyReadOnlyDictionary<string, string>();

		internal Dictionary<string, string> CaptchaSolution { get; } = new Dictionary<string, string>();
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType { get; } = RequestType.Post;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, EditInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			if (input.Text.Length > 8000)
			{
				request.Type = RequestType.PostMultipart;
			}

			foreach (var kvp in this.CaptchaData)
			{
				request.AddHidden(kvp.Key, kvp.Value);
			}

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
				.AddIf("md5", input.Text.GetHash(HashType.Md5), (input.Text ?? (input.PrependText + input.AppendText)) != null)
				.AddHidden("token", input.Token);
		}

		protected override EditResult DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			var output = new EditResult()
			{
				Flags =
				result.GetFlag("new", EditFlags.New) |
				result.GetFlag("nochange", EditFlags.NoChange),
				Result = (string)result["result"],
				PageId = (long?)result["pageid"] ?? 0,
				Title = (string)result["title"],
				ContentModel = (string)result["contentmodel"],
				OldRevisionId = (long?)result["oldrevid"] ?? 0,
				NewRevisionId = (long?)result["newrevid"] ?? 0,
				NewTimestamp = (DateTime?)result["newtimestamp"],
			};
			this.CaptchaData = result["captcha"].AsReadOnlyDictionary<string, string>();

			return output;
		}
		#endregion
	}
}