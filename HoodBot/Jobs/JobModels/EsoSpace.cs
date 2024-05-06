namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using System.IO.Compression;
	using System.Linq;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	internal static class EsoSpace
	{
		#region Static Fields
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

		public static string TimeToText(int time) => ((double)time).ToString("0,.#", CultureInfo.InvariantCulture);
		#endregion

		#region Public WikiJob Extension Methods
		public static void GetIcons(this WikiJob job, string updateFolder, bool pts)
		{
			var downloadPath = IconDownloadPath(updateFolder, pts);
			var localFile = Path.Combine(LocalConfig.BotDataFolder, "icons.zip");
			var extractPath = LocalConfig.WikiIconsFolder;

			if (File.GetLastWriteTime(localFile) < (DateTime.Now - TimeSpan.FromDays(1)))
			{
				job.StatusWriteLine("Updating local icons file");
				job.Site.Download(downloadPath, localFile);

				job.StatusWriteLine("Extracting icons");
				ZipFile.ExtractToDirectory(localFile, extractPath, true);
			}
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

		public static string IconDownloadPath(string updateFolder, bool pts)
		{
			var ptsText = pts ? "pts" : string.Empty;
			return $"https://esofiles.uesp.net/update-{updateFolder}{ptsText}/icons.zip";
		}

		public static void SetBotUpdateVersion(this WikiJob job, string pageType, EsoVersion version)
		{
			ArgumentNullException.ThrowIfNull(job);
			ArgumentException.ThrowIfNullOrEmpty(pageType);
			ArgumentNullException.ThrowIfNull(version);

			job.StatusWriteLine("Update bot parameters");
			var paramName = pageType;
			var patchPage = GetPatchPage(job);
			var parser = new ContextualParser(patchPage);
			if (parser.FindSiteTemplate("Online Patch") is ITemplateNode template && template.Find(paramName) is IParameterNode param)
			{
				param.SetValue(version.Text, ParameterFormat.NoChange);
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
								place.PlaceType = placeInfo.Type;
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
}