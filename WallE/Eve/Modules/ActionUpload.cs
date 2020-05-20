#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Properties;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.CommonCode.Globals;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal class ActionUpload : ActionModule<UploadInputInternal, UploadResult>
	{
		#region Constructors
		public ActionUpload(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 116;

		public override string Name => "upload";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType => RequestType.PostMultipart;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, UploadInputInternal input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));

			// Upload by URL is not implemented due to rarity of use and the level of complexity it adds to the code.
			request
				.AddIfNotNull("filename", input.FileName)
				.AddIfNotNull("comment", input.Comment)
				.AddIfNotNull("text", input.Text)
				.Add("watch", input.Watchlist == WatchlistOption.Watch && this.SiteVersion < 117)
				.Add("unwatch", input.Watchlist == WatchlistOption.Unwatch && this.SiteVersion < 117)
				.AddIfPositiveIf("watchlist", input.Watchlist, this.SiteVersion >= 117)
				.Add("ignorewarnings", input.IgnoreWarnings)
				.Add(input.FileSize > 0 ? "chunk" : "file", input.FileName, input.FileData)
				.AddIfNotNull(this.SiteVersion < 118 ? "sessionkey" : "filekey", input.FileKey)
				.Add("stash", input.Stash)
				.AddIfPositive("offset", input.Offset)
				.AddIfPositive("filesize", input.FileSize)
				.AddHidden("token", input.Token);
		}

		protected override UploadResult DeserializeResult(JToken? result)
		{
			ThrowNull(result, nameof(result));
			var resultText = result.MustHaveString("result");
			IReadOnlyList<string> duplicates = new List<string>();
			var outputWarnings = new Dictionary<string, string>();
			if (result["warnings"] is JToken warnings)
			{
				foreach (var prop in warnings.Children<JProperty>())
				{
					var name = prop.Name;
					var value = prop.Value;
					if (value != null)
					{
						switch (name)
						{
							case "duplicate":
								duplicates = value.GetList<string>();
								break;
							case "nochange":
								if ((string?)value["timestamp"] is string ts)
								{
									outputWarnings.Add(name, ts);
								}

								break;
							default:
								if (value.Type == JTokenType.Object || value.Type == JTokenType.Array)
								{
									this.AddWarning("ActionUpload.DeserializeResult", CurrentCulture(EveMessages.NotAString, name));
								}
								else if ((string?)value is string valueString)
								{
									outputWarnings.Add(name, valueString);
								}

								break;
						}
					}
				}
			}

			return new UploadResult(
				result: resultText,
				duplicates: duplicates,
				fileKey: (string?)result["filekey"],
				fileName: (string?)result["filename"],
				imageInfo: result["imageinfo"] is JToken imageInfoNode ? JTokenImageInfo.ParseImageInfo(imageInfoNode, new ImageInfoItem()) : null,
				warnings: outputWarnings.AsReadOnly());
		}
		#endregion
	}
}