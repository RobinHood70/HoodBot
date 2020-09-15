namespace RobinHood70.HoodBot.Jobs
{
	/*
	using System;
	using System.Collections.Generic;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.Eso;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon; using RobinHood70.CommonCode;
	using RobinHood70.WikiCommon; using RobinHood70.CommonCode;

	public class EsoNpcLocFix : EditJob
	{
		#region Fields
		private readonly PageCollection pages;
		#endregion

		#region Constructors
		[JobInfo("One-Off Job - Fix ESO NPC Locations")]
		public EsoNpcLocFix(JobManager jobManager)
			: base(jobManager) => this.pages = new PageCollection(site);
		#endregion

		#region Public Override Properties
		public override string LogName => "Fix ESO NPC Locations";
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			this.ProgressMaximum = this.pages.Count;
			foreach (var page in this.pages)
			{
				this.SavePage(page, "Remove redundant locations", true);
				this.Progress++;
			}
		}

		protected override void PrepareJob()
		{
			var places = EsoGeneral.GetPlaces(this.Site);
			var allNpcs = EsoGeneral.GetNpcPages(this.Site);
			//// var allNpcs = this.GetNpcSubset();
			foreach (var page in allNpcs)
			{
				var addPage = false;
				var matches = Template.Find("Online NPC Summary").Matches(page.Text);
				if (matches.Count != 1)
				{
					continue;
				}

				var match = matches[0];
				var template = Template.Parse(match.Value);
				var matchAgainst = new TitleCollection(this.Site);
				this.UpdateLocationsFrom(template["city"], page, matchAgainst);
				this.UpdateLocationsFrom(template["settlement"], page, matchAgainst);
				this.UpdateLocationsFrom(template["store"], page, matchAgainst);
				this.UpdateLocationsFrom(template["ship"], page, matchAgainst);
				this.UpdateLocationsFrom(template["house"], page, matchAgainst);
				if (matchAgainst.Count == 0)
				{
					continue;
				}

				var locParameter = template["loc"];
				if (Parameter.IsNullOrEmpty(locParameter))
				{
					continue;
				}

				foreach (var item in matchAgainst)
				{
					if (places.ValueOrDefault(item.PageName) is Place place && place.Zone == null)
					{
						this.WriteLine($"* [[{page.FullPageName}|]]: no zone information. Check <code>loc</code> to see if zone for [[{item.FullPageName}|{item.LabelName}]] needs to be removed.");
					}
				}

				for (var matchNum = 0; matchNum < matchAgainst.Count; matchNum++)
				{
					if (places.ValueOrDefault(matchAgainst[matchNum].PageName) is Place place && place.Zone == null)
					{
						matchAgainst.Add(UespNamespaces.Online, place.Zone);
					}
				}

				matchAgainst.Add(UespNamespaces.Online, "Tamriel");
				var locSplit = new List<string>(locParameter.Value.Split(TextArrays.CommaSpace, StringSplitOptions.None));
				for (var locIndex = locSplit.Count - 1; locIndex >= 0; locIndex--)
				{
					var entry = locSplit[locIndex];
					var link = SiteLink.IsLink(entry) ? new SiteLink(this.Site, entry) : new SiteLink(this.Site, UespNamespaces.Online, entry);
					foreach (ISimpleTitle title in matchAgainst)
					{
						if (link == title || link.DisplayText == title.PageName)
						{
							locSplit.RemoveAt(locIndex);
							addPage = true;
							break;
						}
					}
				}

				if (addPage)
				{
					if (locSplit.Count == 0)
					{
						template.Remove("loc");
					}
					else
					{
						locParameter.Value = string.Join(", ", locSplit);
						this.WriteLine($"* [[{page.FullPageName}|]] has a specific location parameter (city, settlement, etc.), but also has one or more values in the loc parameter that were not removed.");
					}

					page.Text = page.Text
						.Remove(match.Index, match.Length)
						.Insert(match.Index, template.ToString());
				}

				if (addPage)
				{
					this.pages.Add(page);
				}
			}
		}
		#endregion

		#region Protected Methods
		protected PageCollection GetNpcSubset()
		{
			var allTitles = new TitleCollection(this.Site);
			allTitles.SetLimitations(LimitationType.FilterTo, UespNamespaces.Online);
			allTitles.GetPageTranscludedIn(new[] { new Title(this.Site, UespNamespaces.Template, "Online NPC Summary") });
			allTitles.Sort();
			var i = allTitles.Count - 1;
			while (allTitles[i].PageName[0] != 'A')
			{
				allTitles.RemoveAt(i);
				i--;
			}

			var someNpcs = allTitles.Load();
			someNpcs.Sort();
			return someNpcs;
		}
		#endregion

		#region Private Methods
		private void UpdateLocationsFrom(Parameter param, Page page, ICollection<Title> matchAgainst)
		{
			if (Parameter.IsNullOrEmpty(param))
			{
				return;
			}

			var value = param.Value;
			if (value.Contains(",") || value.Contains("<br") || value.Contains("{{") || value.IndexOf("[[", 2, StringComparison.Ordinal) != -1)
			{
				this.WriteLine($"* [[{page.FullPageName}|]]: check <code>{param.Name}</code> manually.");
			}
			else
			{
				if (WikiLink.IsLink(value))
				{
					var link = new WikiLink(value);
					matchAgainst.Add(new Title(this.Site, link.FullPageName));
				}
				else
				{
					matchAgainst.Add(new Title(page.Namespace, value));
				}
			}
		}
		#endregion
	}
	*/
}
