namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

public static class EsoIcons
{
	public static void GetOriginalFiles(Site site) => GetOriginalFiles(site, PageModules.Default);

	public static IReadOnlyDictionary<string, Page> GetOriginalFiles(Site site, PageModules pageModules)
	{
		const string TemplateName = "Online File";

		ArgumentNullException.ThrowIfNull(site);
		var retval = new Dictionary<string, Page>(StringComparer.OrdinalIgnoreCase);

		var pages = new PageCollection(site, pageModules);
		pages.GetBacklinks("Template:" + TemplateName, BacklinksTypes.EmbeddedIn);
		foreach (var page in pages)
		{
			var parser = new SiteParser(page);
			if (parser.FindTemplate(TemplateName) is not ITemplateNode template)
			{
				Debug.WriteLine("Template not found: " + page.Title.FullPageName()); // This should not be possible.
				continue;
			}

			if (template.GetValue("originalfile") is not string originalFileName)
			{
				Debug.WriteLine("Missing originalfileName parameter: " + page.Title.FullPageName()); // Possible, but undesirable.
				continue;
			}

			if (!retval.TryAdd(originalFileName, page))
			{
				Debug.WriteLine("Duplicate files: " + page.Title.PageName + " and " + retval[originalFileName].Title.PageName);
			}
		}

		return retval;
	}

	public static string SanitizeFileName(string paramValue)
	{
		paramValue = paramValue.Replace("<br>", string.Empty, StringComparison.OrdinalIgnoreCase);
		if (paramValue.Length > 0 && paramValue[0] == '/')
		{
			paramValue = paramValue[1..];
		}

		if (paramValue.StartsWith("esoui/art/", StringComparison.OrdinalIgnoreCase))
		{
			paramValue = paramValue[10..];
		}

		var split = paramValue.Split('.', 2);
		var ext = split.Length > 1 ? split[1] : string.Empty;
		if (string.Equals(ext, "png", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(ext, "dds", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(ext, "jpg", StringComparison.OrdinalIgnoreCase))
		{
			paramValue = split[0];
		}

		return paramValue;
	}
}