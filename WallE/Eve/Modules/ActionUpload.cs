#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Properties.EveMessages;
	using static RobinHood70.WikiCommon.Globals;

	internal class ActionUpload : ActionModule<UploadInputInternal, UploadResult>
	{
		#region Fields
		private bool continued = false;
		#endregion

		#region Constructors
		public ActionUpload(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 116;

		public override string Name { get; } = "upload";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType { get; } = RequestType.PostMultipart;

		protected override StopCheckMethods StopMethods => this.continued ? StopCheckMethods.None : this.Wal.StopCheckMethods;
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

		protected override UploadResult DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			var output = new UploadResult()
			{
				Result = (string)result["result"],
			};

			// Disallow stop checks while upload is in progress.
			this.continued = output.Result == "Continued";
			output.FileName = (string)result["filename"];

			var outputWarnings = new Dictionary<string, string>();
			var warnings = result["warnings"];
			if (warnings != null)
			{
				foreach (var prop in warnings.Children<JProperty>())
				{
					var name = prop.Name;
					var value = prop.Value;
					switch (name)
					{
						case "duplicate":
							output.Duplicates = value.AsReadOnlyList<string>();
							break;
						case "nochange":
							var ts = (string)value["timestamp"];
							outputWarnings.Add(name, ts);
							break;
						default:
							if (value.Type == JTokenType.Object || value.Type == JTokenType.Array)
							{
								this.AddWarning("ActionUpload.DeserializeResult", CurrentCulture(NotAString, name));
							}
							else
							{
								outputWarnings.Add(name, (string)value);
							}

							break;
					}
				}
			}

			output.Warnings = outputWarnings.AsReadOnly();
			output.FileKey = (string)result["filekey"];

			var imageInfo = result["imageinfo"];
			if (imageInfo != null)
			{
				output.ImageInfo = result["imageinfo"].ParseImageInfo();
			}

			return output;
		}
		#endregion
	}
}