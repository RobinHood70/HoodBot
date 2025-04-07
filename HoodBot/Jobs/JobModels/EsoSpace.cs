namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Uesp;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon.Parser;

internal static class EsoSpace
{
	#region Static Fields
	private static readonly char[] CommaOrSpace = [',', ' '];
	private static VariablesPage? patchVarPage;
	#endregion

	#region Public Properties
	public static IReadOnlyList<PlaceInfo> PlaceInfo { get; } =
	[
		new PlaceInfo(PlaceType.City, "city", "Online-Places-Cities", 5),
		new PlaceInfo(PlaceType.Settlement, "settlement", "Online-Places-Settlements", 5),
		new PlaceInfo(PlaceType.House, "house", "Online-Places-Homes", 1),
		new PlaceInfo(PlaceType.Ship, "ship", "Online-Places-Ships", 1),
		new PlaceInfo(PlaceType.Store, "store", "Online-Places-Stores", 1),
		new PlaceInfo(PlaceType.Unknown, "loc", null, 10),
	];
	#endregion

	#region Public Methods
	public static ITemplateNode FindOrCreateOnlineFile(SiteParser parser, params string[] originalFileNames)
	{
		ArgumentNullException.ThrowIfNull(parser);
		if (parser.FindTemplate("Online File") is not ITemplateNode template)
		{
			template = parser.Factory.TemplateNodeFromWikiText("{{Online File\n|originalfile=\n}}");
			parser.Insert(0, template);
			parser.InsertText(1, "\n\n");
		}

		if (!template.TitleNodes.ToRaw().EndsWith('\n'))
		{
			template.TitleNodes.AddText("\n");
		}

		if (originalFileNames?.Length > 0)
		{
			var fileNames = new SortedSet<string>(originalFileNames, StringComparer.Ordinal);
			if (template.Find("originalfile") is not IParameterNode fileParam)
			{
				fileParam = template.Factory.ParameterNodeFromParts("originalfile", string.Empty);
			}

			var existing = fileParam.GetValue();
			fileNames.UnionWith(existing.Split(TextArrays.Comma));
			fileNames.Remove(string.Empty);
			fileParam.SetValue(string.Join(',', fileNames), ParameterFormat.OnePerLine);
		}

		return template;
	}

	public static void AddToOnlineFile(ITemplateNode template, string linkType, string linkValue) =>
		AddToOnlineFile(template, (linkType, linkValue));

	public static void AddToOnlineFile(ITemplateNode template, params (string Type, string Value)[] links) =>
		AddToOnlineFile(template, (IList<(string Type, string Value)>)links);

	public static void AddToOnlineFile(ITemplateNode template, IList<(string Type, string Value)> links)
	{
		// CONSIDER: This is kludgy - it clears and reinserts the entire parameter list every time to ensure correct format and sorting. Might want to rewrite as separate routines that get the SortedSet (or even revert to a list), clear the anonymous parameters, and update from list. That way, caller can optimize flow as needed.
		ArgumentNullException.ThrowIfNull(template);
		if (links is null || links.Count == 0)
		{
			return;
		}

		var anons = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var cluster in template.ParameterCluster(2))
		{
			var key = cluster[0].GetRaw();
			var value = cluster[1].GetRaw();
			anons.Add(key + '\t' + value);
		}

		for (var i = template.Parameters.Count - 1; i >= 0; i--)
		{
			if (template.Parameters[i].Anonymous)
			{
				template.Parameters.RemoveAt(i);
			}
		}

		foreach (var item in anons)
		{
			var split = item.Split('\t');
			template.Add(split[0], ParameterFormat.Packed);
			template.Add(split[1], ParameterFormat.OnePerLine);
		}
	}

	public static PlaceCollection GetPlaces(Site site)
	{
		ArgumentNullException.ThrowIfNull(site);
		var places = site.CreateMetaPageCollection(PageModules.None, true, "alliance", "settlement", "titlename", "type", "zone");
		places.SetLimitations(LimitationType.OnlyAllow, UespNamespaces.Online);
		places.GetCategoryMembers("Online-Places");

		PlaceCollection retval = new();
		foreach (var page in places.OfType<VariablesPage>())
		{
			if (page.MainSet != null)
			{
				retval.Add(new Place(page));
			}
		}

		foreach (var mappedName in places.TitleMap)
		{
			// TODO: Take another look at this later. Error catching added here that triggered on [[Online:Hircine's Hunting Grounds]]. Having a bad day and not sure if this is the right thing to do.
			try
			{
				if (retval[mappedName.Value.Title.PageName] is Place place)
				{
					// In an ideal world, this would be a direct reference to the same place, rather than a copy, but that ends up being a lot of work for very little gain.
					var key = TitleFactory.FromUnvalidated(site, mappedName.Key).PageName;
					retval.Add(Place.Copy(key, place));
				}
			}
			catch (InvalidOperationException)
			{
				// Do nothing
			}
		}

		foreach (var placeInfo in PlaceInfo)
		{
			GetPlaceCategory(site, retval, placeInfo);
		}

		return retval;
	}

	public static bool ShouldUpload(string localFileName, FilePage filePage)
	{
		// Assumes that FileInfo with Sha1 has been downloaded for page.
		if (filePage.Exists)
		{
			var stream = File.OpenRead(localFileName);
			var hashString = Globals.GetHash(stream, HashType.Sha1);
			foreach (var fileRevision in filePage.FileRevisions)
			{
				if (fileRevision.Sha1.OrdinalEquals(hashString))
				{
					return false;
				}
			}
		}

		return true;
	}

	public static string TimeToText(int time) => time == -1 ? string.Empty : ((double)time).ToString("0,.#", CultureInfo.InvariantCulture);

	public static string? TrimBehavior(string? behavior) => behavior?
		.Trim()
		.Replace(",,", ",", StringComparison.Ordinal)
		.Trim(CommaOrSpace);
	#endregion

	#region Public WikiJob Extension Methods
	public static Dictionary<string, HashSet<string>> GetIconChecksums()
	{
		var allIcons = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
		if (Directory.Exists(LocalConfig.WikiIconsFolder))
		{
			foreach (var file in Directory.EnumerateFiles(LocalConfig.WikiIconsFolder, "*.*", SearchOption.AllDirectories))
			{
				var fileData = File.ReadAllBytes(file);
				var checksum = Globals.GetHash(fileData, HashType.Sha1);
				if (!allIcons.TryGetValue(checksum, out var list))
				{
					list = new HashSet<string>(1, StringComparer.Ordinal);
					allIcons.Add(checksum, list);
				}

				list.Add(file[LocalConfig.WikiIconsFolder.Length..].Replace(".png", string.Empty, StringComparison.OrdinalIgnoreCase).Replace('\\', '/'));
			}
		}

		return allIcons;
	}

	public static EsoVersion GetPatchVersion(this WikiJob job, string paramName)
	{
		ArgumentException.ThrowIfNullOrEmpty(paramName);
		var patchPage = GetPatchPage(job);
		var version = patchPage.GetVariable(paramName);
		return version is null
			? throw new InvalidOperationException($"Patch variable \"{paramName}\" not found")
			: EsoVersion.FromText(version);
	}

	public static void SetBotUpdateVersion(this WikiJob job, string pageType, EsoVersion version)
	{
		ArgumentNullException.ThrowIfNull(job);
		ArgumentException.ThrowIfNullOrEmpty(pageType);
		ArgumentNullException.ThrowIfNull(version);

		job.StatusWriteLine("Update bot parameters");
		var paramName = pageType;
		var patchPage = GetPatchPage(job);
		var parser = new SiteParser(patchPage);
		if (parser.FindTemplate("Online Patch") is ITemplateNode template && template.Find(paramName) is IParameterNode param)
		{
			param.SetValue(version.Text, ParameterFormat.OnePerLine);
			parser.UpdatePage();
			patchPage.Save("Update " + paramName, true);
		}
	}
	#endregion

	#region Private Methods
	private static VariablesPage GetPatchPage(WikiJob job)
	{
		if (patchVarPage is null)
		{
			job.StatusWriteLine("Fetching ESO update number");
			TitleCollection patchTitle = new(job.Site, "Online:Patch");
			var pages = job.Site.CreateMetaPageCollection(PageModules.Default, false);
			pages.GetTitles(patchTitle);
			patchVarPage = pages.Count == 1 && pages[0] is VariablesPage varPage
				? varPage
				: throw new InvalidOperationException("Could not find patch page.");
		}

		return patchVarPage;
	}

	private static void GetPlaceCategory(Site site, PlaceCollection places, PlaceInfo placeInfo)
	{
		if (placeInfo.CategoryName == null)
		{
			return;
		}

		PageCollection cat = new(site);
		cat.GetCategoryMembers(placeInfo.CategoryName);
		foreach (var member in cat)
		{
			if (member.Title.Namespace == UespNamespaces.Online)
			{
				// TODO: Take another look at this later. Error catching added here that triggered on [[Online:Farm House]]. Having a bad day and not sure if this is the right thing to do.
				try
				{
					if (places[member.Title.PageName] is Place place)
					{
						if (place.PlaceType == PlaceType.Unknown)
						{
							place.PlaceType = placeInfo.PlaceType;
						}
						else
						{
							Debug.WriteLine($"Multiple place types on page: {member.Title.FullPageName()}");
						}
					}
				}
				catch (InvalidOperationException)
				{
					// Do nothing
				}
			}
			else if (member.Title.Namespace != UespNamespaces.Category)
			{
				Debug.WriteLine($"Unexpected page [[{member.Title.FullPageName()}]] found in [[:Category:{placeInfo.CategoryName}]].");
			}
		}
	}
	#endregion
}