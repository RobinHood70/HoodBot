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
		public static T ParseImageInfo<T>(JToken result, T item)
			where T : ImageInfoItem
		{
			item.Timestamp = (DateTime?)result["timestamp"];
			item.User = (string?)result["user"];
			item.UserId = (long?)result["userid"] ?? -1;
			item.Flags =
				result.GetFlag("anon", ImageInfoFlags.Anonymous) |
				result.GetFlag("commenthidden", ImageInfoFlags.CommentHidden) |
				result.GetFlag("filehidden", ImageInfoFlags.FileHidden) |
				result.GetFlag("suppressed", ImageInfoFlags.Suppressed) |
				result.GetFlag("userhidden", ImageInfoFlags.UserHidden);
			item.Size = (int?)result["size"] ?? 0;
			item.Width = (short?)result["width"] ?? 0;
			item.Height = (short?)result["height"] ?? 0;
			item.PageCount = (long?)result["pagecount"] ?? 0;
			item.Duration = (float?)result["duration"] ?? 0;
			item.Comment = (string?)result["comment"] ?? (string?)result["description"];
			item.ParsedComment = (string?)result["parsedcomment"] ?? (string?)result["parseddescription"];
			item.UploadWarningHtml = (string?)result["html"];
			item.CanonicalTitle = (string?)result["canonicaltitle"];
			item.ThumbUri = (string?)result["thumburl"];
			item.ThumbWidth = (int?)result["thumbwidth"] ?? 0;
			item.ThumbHeight = (int?)result["thumbheight"] ?? 0;
			item.ThumbMime = (string?)result["thumbmime"];
			item.ThumbError = (string?)result["thumberror"];
			item.Uri = (string?)result["url"];
			item.DescriptionUri = (string?)result["descriptionurl"];
			item.Sha1 = (string?)result["sha1"];
			item.MimeType = (string?)result["mime"];
			item.MediaType = (string?)result["mediatype"];
			item.ArchiveName = (string?)result["archivename"];
			item.BitDepth = (int?)result["bitdepth"] ?? 0;

			FillMetadata(result["metadata"], item.Metadata);
			FillMetadata(result["commonmetadata"], item.CommonMetadata);
			FillExtendedMetadata(result["extmetadata"], item.ExtendedMetadata);

			return item;
		}
		#endregion

		#region Private Methods
		private static void FillMetadata(JToken? tree, IReadOnlyDictionary<string, object> dictionary)
		{
			if (dictionary is Dictionary<string, object> dict && tree != null)
			{
				dict.Clear();
				foreach (var node in tree)
				{
					ParseMetadataNode(node, dict);
				}
			}
		}

		private static void FillExtendedMetadata(JToken? list, IReadOnlyDictionary<string, ExtendedMetadataItem> dictionary)
		{
			if (dictionary is Dictionary<string, ExtendedMetadataItem> dict && list != null)
			{
				foreach (var item in list.Children<JProperty>())
				{
					var name = item.Name;
					var itemValue = item.Value;
					var value = itemValue.NotNull("value");
					var source = itemValue.SafeString("source");
					var hidden = itemValue["hidden"].AsBCBool();
					if (value.Type == JTokenType.Object)
					{
						var newItem = new ExtendedMetadataItem(value.AsReadOnlyDictionary<string>(), source, hidden);
						dict.Add(name, newItem);
					}
					else
					{
						var stringValue = (string?)value;
						if (stringValue != null)
						{
							var newDict = new Dictionary<string, string> { [string.Empty] = stringValue };
							dict.Add(name, new ExtendedMetadataItem(newDict, source, hidden));
						}
					}
				}
			}
		}

		private static void ParseMetadataNode(JToken node, Dictionary<string, object> dict)
		{
			var name = (string?)node["name"];
			var value = node["value"];
			if (name != null && value != null)
			{
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
					var stringValue = (string?)value;
					if (stringValue != null)
					{
						dict.Add(name, stringValue);
					}
				}
			}
		}
		#endregion
	}
}