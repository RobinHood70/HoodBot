namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Models;
using RobinHood70.HoodBot.Uesp;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

internal sealed class MorrowindBooksInterlanguageLinks : ParsedPageJob
{
	#region Static Fields
	private static readonly Regex IdFields = new(@"^id\d*", RegexOptions.None, Globals.DefaultRegexTimeout);
	#endregion

	#region Fields
	private readonly string baseLang;
	private readonly Dictionary<string, string> templateNames = new(StringComparer.OrdinalIgnoreCase)
	{
		["en"] = "Game Book",
		["fr"] = "Livre de jeu",
	};

	private readonly string editSummary;
	private readonly Dictionary<string, Title> otherIds = new(StringComparer.OrdinalIgnoreCase);
	private readonly string otherLang;
	private readonly Site otherSite;
	#endregion

	#region Constructors
	[JobInfo("MW Books Interlanguage Links", "Morrowind")]
	public MorrowindBooksInterlanguageLinks(JobManager jobManager)
		: base(jobManager)
	{
		this.Pages.FilterToNamespaces(UespNamespaces.Morrowind);
		var hostSplit = SplitHost(jobManager.WikiInfo.Api);
		this.baseLang = hostSplit[0];
		this.otherLang = this.baseLang switch
		{
			"en" => "fr",
			"fr" => "en",
			_ => throw new NotSupportedException()
		};

		var wikiInfo = JobManager.FindWikiInfo(wi => wi.Api?.Host.OrdinalICEquals(this.otherLang + '.' + hostSplit[1]) ?? false) ?? throw new InvalidOperationException($"Could not find wiki info for language '{this.otherLang}'");
		var otherWal = jobManager.CreateAbstractionLayer(wikiInfo);
		this.otherSite = jobManager.CreateSite(wikiInfo, otherWal, true);
		this.otherSite.Login(wikiInfo.UserName, wikiInfo.Password);
		this.editSummary = this.baseLang switch
		{
			"en" => "Add interlanguage link",
			"fr" => "Ajouter un lien interlangue",
			_ => throw new NotSupportedException()
		};
	}
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => this.editSummary;

	protected override void LoadPages()
	{
		var otherPages = new PageCollection(this.otherSite);
		this.GetBacklinks(otherPages, this.otherLang);
		foreach (var page in otherPages)
		{
			var parser = new SiteParser(page);
			var templates = this.FilterTemplates(parser, this.otherLang);
			foreach (var template in parser.FindTemplates(this.Site, this.templateNames[this.otherLang]))
			{
				foreach (var id in GetIds(template))
				{
					if (!this.otherIds.TryAdd(id, page.Title))
					{
						Debug.WriteLine($"ID '{id}' is not unique, skipping. (Pages: {page.Title}, {this.otherIds[id]})");
					}
				}
			}
		}

		this.GetBacklinks(this.Pages, this.baseLang);
	}

	protected override void PageLoaded(Page page)
	{
		if (this.baseLang.OrdinalEquals("fr"))
		{
			var templateSplit = page.Text.Split("}}", 2, StringSplitOptions.None);
			if (templateSplit.Length == 2)
			{
				var lines = templateSplit[1].Split('\n', StringSplitOptions.None);
				for (var i = 0; i < (lines.Length - 1); i++)
				{
					lines[i] = lines[i] + '\n';
				}

				var startLine = 0;
				while (startLine < lines.Length && lines[startLine].Trim().Length == 0)
				{
					startLine++;
				}

				if (startLine < lines.Length)
				{
					var pattern = @"^(?<template>{{Centre\|\s*)?\*?" + Regex.Escape(page.Title.PageName) + @"(\*|<br>)?\n";
					lines[startLine] = Regex.Replace(lines[startLine], pattern, "${template}", RegexOptions.IgnoreCase, Globals.DefaultRegexTimeout);
					page.Text = templateSplit[0] + "}}" + string.Join(string.Empty, lines);
					page.Text = Regex.Replace(page.Text, @"{{Centre\|\s*}}\s*", string.Empty, RegexOptions.None, Globals.DefaultRegexTimeout);
				}
			}
		}

		base.PageLoaded(page);
	}

	protected override void ParseText(SiteParser parser)
	{
		foreach (var link in parser.LinkNodes)
		{
			var linkTarget = TitleFactory.FromUnvalidated(this.Site, link.GetTitleText());
			if (linkTarget.Interwiki?.Language is not null)
			{
				return;
			}
		}

		var bookTemplates = new List<ITemplateNode>(this.FilterTemplates(parser, this.baseLang));
		if (bookTemplates.Count == 0)
		{
			return;
		}

		var otherTitle = this.FindOtherTitle(bookTemplates);
		if (otherTitle is null)
		{
			Debug.WriteLine($"ID mismatch on {parser.Title}, skipping.");
			return;
		}

		var langLink = $"{this.otherLang}:{otherTitle.FullPageName()}";
		if (parser.FindLink(langLink) is not null)
		{
			return;
		}

		parser.AddParsed($"\n[[{langLink}]]");

		return;
	}
	#endregion

	#region Private Static Methods
	private static IEnumerable<string> GetIds(ITemplateNode template)
	{
		foreach (var param in template.Parameters.Where(p => IdFields.IsMatch(p.GetName() ?? string.Empty)))
		{
			var paramValue = param.GetValue();
			var brSplit = paramValue.Split("<br>", StringSplitOptions.TrimEntries);
			foreach (var entry in brSplit)
			{
				yield return entry
					.Split("''", 2)[0]
					.Split(" (", 2)[0];
			}
		}
	}

	private static string[] SplitHost(Uri? uri)
	{
		ArgumentNullException.ThrowIfNull(uri);
		var apiSplit = uri.Host.Split('.', 2);
		return apiSplit;
	}
	#endregion

	#region Private Methods
	private IEnumerable<ITemplateNode> FilterTemplates(SiteParser parser, string lang) => parser
		.FindTemplates(this.Site, this.templateNames[lang])
		.Where(t => !(t.GetValue("scroll") ?? string.Empty).OrdinalEquals("1"));

	private Title? FindOtherTitle(List<ITemplateNode> bookTemplates)
	{
		foreach (var template in bookTemplates)
		{
			foreach (var id in GetIds(template))
			{
				if (this.otherIds.TryGetValue(id, out var idTitle))
				{
					return idTitle;
				}
			}
		}

		return null;
	}

	private void GetBacklinks(PageCollection pages, string lang)
	{
		var templateName = "Template:" + this.templateNames[lang];
		pages.GetBacklinks(templateName, BacklinksTypes.EmbeddedIn, false, Filter.Exclude, UespNamespaces.Morrowind);
		pages.GetBacklinks(templateName, BacklinksTypes.EmbeddedIn, false, Filter.Exclude, UespNamespaces.Tribunal);
		pages.GetBacklinks(templateName, BacklinksTypes.EmbeddedIn, false, Filter.Exclude, UespNamespaces.Bloodmoon);
	}
	#endregion
}