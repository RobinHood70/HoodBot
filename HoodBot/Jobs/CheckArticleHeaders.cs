﻿namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon.Parser;

	public class CheckArticleHeaders : WikiJob
	{
		private static readonly HashSet<string> BadHeaders = new HashSet<string>
		{
			"Bug", "Map", "Note", "Quest", "Quests", "Related Quest"
		};

		private readonly Dictionary<string, TitleCollection> exceptions = new Dictionary<string, TitleCollection>();
		private readonly int ns;

		[JobInfo("Check Article Headers", "Maintenance|")]
		public CheckArticleHeaders([NotNull, ValidatedNotNull] Site site, [NotNull, ValidatedNotNull] AsyncInfo asyncInfo)
			: base(site, asyncInfo) => this.ns = UespNamespaces.Online;

		protected override void Main()
		{
			this.LoadExceptionsForNamespace();
			var pages = new PageCollection(this.Site);
			pages.GetNamespace(this.ns, Filter.Exclude);
			pages.Sort();
			foreach (var page in pages)
			{
				var parsedPage = WikiTextParser.Parse(page.Text);
				foreach (var node in parsedPage.FindAll<HeaderNode>())
				{
					var header = node.GetInnerText(true);
					if (BadHeaders.Contains(header) && !this.IsException(page, header))
					{
						this.WriteLine($"* {page.AsLink(false)} has a {header} header.");
					}
				}
			}
		}

		private void LoadExceptionsForNamespace()
		{
			switch (this.ns)
			{
				case UespNamespaces.Online:
					var titles = this.EnsureExceptionGroup("Quests");
					titles.GetCategoryMembers("Online-Places-Zones");
					titles.GetCategoryMembers("Online-Achievements-DLC Achievements");
					titles.GetCategoryMembers("Online-Achievements by Zone", true);
					break;
			}
		}

		private TitleCollection EnsureExceptionGroup(string group)
		{
			if (!this.exceptions.TryGetValue(group, out var titles))
			{
				titles = new TitleCollection(this.Site);
				this.exceptions.Add(group, titles);
			}

			return titles;
		}

		private bool IsException(Page page, string header)
		{
			if (header == "Quests" && page.PageName.StartsWith("Patch/", StringComparison.Ordinal))
			{
				return true;
			}

			if (this.exceptions.TryGetValue(header, out var exceptionGroup))
			{
				if (exceptionGroup.Contains(page))
				{
					return true;
				}
			}

			return false;
		}
	}
}