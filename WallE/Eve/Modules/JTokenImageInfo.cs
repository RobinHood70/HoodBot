#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;

	internal static class JTokenImageInfo
	{
		#region Public Methods
		public static ImageInfoItem ParseImageInfo(this JToken result) => ParseImageInfo(result, new ImageInfoItem());

		public static ImageInfoItem ParseImageInfo(this JToken result, ImageInfoItem imageInfo)
		{
			imageInfo.Timestamp = (DateTime?)result["timestamp"];
			imageInfo.User = (string)result["user"];
			imageInfo.UserId = (long?)result["userid"] ?? -1;
			imageInfo.Flags =
				result.GetFlag("anon", ImageInfoFlags.Anonymous) |
				result.GetFlag("commenthidden", ImageInfoFlags.CommentHidden) |
				result.GetFlag("filehidden", ImageInfoFlags.FileHidden) |
				result.GetFlag("suppressed", ImageInfoFlags.Suppressed) |
				result.GetFlag("userhidden", ImageInfoFlags.UserHidden);
			imageInfo.Size = (int?)result["size"] ?? 0;
			imageInfo.Width = (short?)result["width"] ?? 0;
			imageInfo.Height = (short?)result["height"] ?? 0;
			imageInfo.PageCount = (long?)result["pagecount"] ?? 0;
			imageInfo.Duration = (float?)result["duration"] ?? 0;
			imageInfo.Comment = (string)result["comment"] ?? (string)result["description"];
			imageInfo.ParsedComment = (string)result["parsedcomment"] ?? (string)result["parseddescription"];
			imageInfo.UploadWarningHtml = (string)result["html"];
			imageInfo.CanonicalTitle = (string)result["canonicaltitle"];
			imageInfo.ThumbUri = (string)result["thumburl"];
			imageInfo.ThumbWidth = (int?)result["thumbwidth"] ?? 0;
			imageInfo.ThumbHeight = (int?)result["thumbheight"] ?? 0;
			imageInfo.ThumbMime = (string)result["thumbmime"];
			imageInfo.ThumbError = (string)result["thumberror"];
			imageInfo.Uri = (string)result["url"];
			imageInfo.DescriptionUri = (string)result["descriptionurl"];
			imageInfo.Sha1 = (string)result["sha1"];
			imageInfo.Metadata = result["metadata"].AsMetadataTree();
			imageInfo.CommonMetadata = result["commonmetadata"].AsMetadataTree();
			imageInfo.ExtendedMetadata = result["extmetadata"].AsExtendedMetadata();
			imageInfo.MimeType = (string)result["mime"];
			imageInfo.MediaType = (string)result["mediatype"];
			imageInfo.ArchiveName = (string)result["archivename"];
			imageInfo.BitDepth = (int?)result["bitdepth"] ?? 0;

			return imageInfo;
		}
		#endregion

		#region Private Methods
		private static Dictionary<string, object> AsMetadataTree(this JToken tree)
		{
			var dict = new Dictionary<string, object>();
			if (tree != null)
			{
				foreach (var node in tree)
				{
					ParseMetadataNode(node, dict);
				}
			}

			return dict;
		}

		private static Dictionary<string, ExtendedMetadataItem> AsExtendedMetadata(this JToken list)
		{
			var dict = new Dictionary<string, ExtendedMetadataItem>();
			if (list != null)
			{
				foreach (var item in list.Children<JProperty>())
				{
					var name = item.Name;
					var itemValue = item.Value;
					var value = itemValue["value"];
					var source = (string)itemValue["source"];
					var hidden = itemValue["hidden"].AsBCBool();
					if (value.Type == JTokenType.Object)
					{
						dict.Add(name, new ExtendedMetadataItem(value.AsReadOnlyDictionary<string, string>(), source, hidden));
					}
					else
					{
						var newdict = new Dictionary<string, string> { [string.Empty] = (string)value };
						dict.Add(name, new ExtendedMetadataItem(newdict, source, hidden));
					}
				}
			}

			return dict;
		}

		private static void ParseMetadataNode(JToken node, Dictionary<string, object> dict)
		{
			var name = (string)node["name"];
			var value = node["value"];
			if (value.Type == JTokenType.Array)
			{
				var newDict = new Dictionary<string, object>();
				foreach (var item in value)
				{
					ParseMetadataNode(item, newDict);
				}

				dict.Add(name, newDict);
			}
			else
			{
				dict.Add(name, (string)value);
			}
		}
		#endregion
	}
}