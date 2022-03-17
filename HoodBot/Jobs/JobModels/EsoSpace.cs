namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.Linq;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	internal static class EsoSpace
	{
		#region Fields
		private static readonly Regex BonusFinder = new(@"\s*Current [Bb]onus:.*?\.", RegexOptions.None, Globals.DefaultRegexTimeout);
		private static string? patchVersion;
		#endregion

		#region Public Properties
		public static IReadOnlyList<PlaceInfo> PlaceInfo { get; } = new PlaceInfo[]
		{
			new PlaceInfo(PlaceType.City, "city", "Online-Places-Cities", 5),
			new PlaceInfo(PlaceType.Settlement, "settlement", "Online-Places-Settlements", 5),
			new PlaceInfo(PlaceType.House, "house", "Online-Places-Homes", 1),
			new PlaceInfo(PlaceType.Ship, "ship", "Online-Places-Ships", 1),
			new PlaceInfo(PlaceType.Store, "store", "Online-Places-Stores", 1),
			new PlaceInfo(PlaceType.Unknown, "loc", null, 10),
		};
		#endregion

		#region Public Methods
		public static string GetPatchVersion(WikiJob job)
		{
			if (patchVersion == null)
			{
				GetPatchPage(job);
			}

			return patchVersion!;
		}

		public static PlaceCollection GetPlaces(Site site)
		{
			var places = site.NotNull(nameof(site)).CreateMetaPageCollection(PageModules.None, true, "alliance", "settlement", "titlename", "type", "zone");
			places.SetLimitations(LimitationType.FilterTo, UespNamespaces.Online);
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
					if (retval[mappedName.Value.PageName] is Place place)
					{
						// In an ideal world, this would be a direct reference to the same place, rather than a copy, but that ends up being a lot of work for very little gain.
						var key = CreateTitle.FromUnvalidated(site, mappedName.Key).PageName;
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

		public static string HarmonizeDescription(string desc) => RegexLibrary.WhitespaceToSpace(BonusFinder.Replace(desc, string.Empty));

		public static void SetBotUpdateVersion(WikiJob job, string pageType)
		{
			// Assumes EsoPatchVersion has already been updated.
			job.StatusWriteLine("Update patch bot parameters");
			var patchPage = GetPatchPage(job);
			ContextualParser parser = new(patchPage);
			var paramName = "bot" + pageType.NotNull(nameof(pageType));
			if (parser.FindSiteTemplate("Online Patch") is ITemplateNode template && template.Find(paramName) is IParameterNode param)
			{
				param.Value.Clear();
				param.Value.AddText(GetPatchVersion(job) + '\n');
				parser.UpdatePage();
				patchPage.Save("Update " + paramName, true);
			}
		}

		public static string TimeToText(int time) => ((double)time).ToString("0,.#", CultureInfo.InvariantCulture);
		#endregion

		#region Private Methods
		private static VariablesPage GetPatchPage(WikiJob job)
		{
			job.StatusWriteLine("Fetching ESO update number");
			TitleCollection patchTitle = new(job.Site, "Online:Patch");
			var pages = job.Site.CreateMetaPageCollection(PageModules.Default, false);
			pages.GetTitles(patchTitle);
			if (pages.Count == 1
				&& pages[0] is VariablesPage patchPage
				&& patchPage.MainSet?["number"] is string version)
			{
				patchVersion = version;
				return patchPage;
			}

			throw new InvalidOperationException("Could not find patch version on page.");
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
				if (member.Namespace == UespNamespaces.Online)
				{
					// TODO: Take another look at this later. Error catching added here that triggered on [[Online:Farm House]]. Having a bad day and not sure if this is the right thing to do.
					try
					{
						if (places[member.PageName] is Place place)
						{
							if (place.PlaceType == PlaceType.Unknown)
							{
								place.PlaceType = placeInfo.Type;
							}
							else
							{
								Debug.WriteLine($"Multiple place types on page: {member.FullPageName}");
							}
						}
					}
					catch (InvalidOperationException)
					{
						// Do nothing
					}
				}
				else if (member.Namespace != UespNamespaces.Category)
				{
					Debug.WriteLine($"Unexpected page [[{member.FullPageName}]] found in [[:Category:{placeInfo.CategoryName}]].");
				}
			}
		}
		#endregion
	}
}