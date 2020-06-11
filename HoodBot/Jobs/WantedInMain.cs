﻿namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;

	public class WantedInMain : WikiJob
	{
		#region Constructors
		[JobInfo("Wanted Pages in Main Space", "Maintenance")]
		public WantedInMain([NotNull, ValidatedNotNull] Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
		}
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			var titles = new TitleCollection(this.Site);
			titles.GetQueryPage("Wantedpages");
			var sorted = new List<string>();
			foreach (var title in titles)
			{
				if (title.Namespace == MediaWikiNamespaces.Main)
				{
					var uri = Uri.EscapeUriString(title.FullPageName()).Replace("?", "%3F", StringComparison.Ordinal);
					sorted.Add($"* [https://en.uesp.net/wiki/Special:WhatLinksHere/{uri} {title.FullPageName()}]");
				}
			}

			sorted.Sort();
			foreach (var item in sorted)
			{
				this.WriteLine(item);
			}
		}
		#endregion
	}
}