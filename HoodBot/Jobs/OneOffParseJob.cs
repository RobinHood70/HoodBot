namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

internal sealed class OneOffParseJob : ParsedPageJob
{
	private readonly Dictionary<string, string> replacements;

	[JobInfo("One-Off Parse Job")]
	public OneOffParseJob(JobManager jobManager)
		: base(jobManager)
	{
		// I just remembered there's a better way of doing this using a PageCollection, but this is written and working, so not gonna spend time on it.
		this.replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["Online:Guild Reprint: Alik'r Desert Lore"] = "{{Item Link|Guild Reprint: Alik'r Desert Lore|id=120381|quality=f}}",
			["Online:Guild Reprint: Auridon Lore"] = "{{Item Link|Guild Reprint: Auridon Lore|id=120401|quality=f}}",
			["Online:Guild Reprint: Bangkorai Lore"] = "{{Item Link|Guild Reprint: Bangkorai Lore|id=120380|quality=f}}",
			["Online:Guild Reprint: Biographies"] = "{{Item Link|Guild Reprint: Biographies|id=120385|quality=f}}",
			["Online:Guild Reprint: Coldharbour Lore"] = "{{Item Link|Guild Reprint: Coldharbour Lore|id=120405|quality=f}}",
			["Online:Guild Reprint: Daedric Princes"] = "{{Item Link|Guild Reprint: Daedric Princes|id=120384|quality=f}}",
			["Online:Guild Reprint: Deshaan Lore"] = "{{Item Link|Guild Reprint: Deshaan Lore|id=120399|quality=f}}",
			["Online:Guild Reprint: Divines and Deities"] = "{{Item Link|Guild Reprint: Divines and Deities|id=120386|quality=f}}",
			["Online:Guild Reprint: Dungeon Lore"] = "{{Item Link|Guild Reprint: Dungeon Lore|id=120387|quality=f}}",
			["Online:Guild Reprint: Dwemer"] = "{{Item Link|Guild Reprint: Dwemer|id=120388|quality=f}}",
			["Online:Guild Reprint: Eastmarch Lore"] = "{{Item Link|Guild Reprint: Eastmarch Lore|id=120398|quality=f}}",
			["Online:Guild Reprint: Glenumbra Lore"] = "{{Item Link|Guild Reprint: Glenumbra Lore|id=120377|quality=f}}",
			["Online:Guild Reprint: Grahtwood Lore"] = "{{Item Link|Guild Reprint: Grahtwood Lore|id=120402|quality=f}}",
			["Online:Guild Reprint: Greenshade Lore"] = "{{Item Link|Guild Reprint: Greenshade Lore|id=120403|quality=f}}",
			["Online:Guild Reprint: Legends of Nirn"] = "{{Item Link|Guild Reprint: Legends of Nirn|id=120389|quality=f}}",
			["Online:Guild Reprint: Literature"] = "{{Item Link|Guild Reprint: Literature|id=120390|quality=f}}",
			["Online:Guild Reprint: Magic and Magicka"] = "{{Item Link|Guild Reprint: Magic and Magicka|id=120391|quality=f}}",
			["Online:Guild Reprint: Malabal Tor Lore"] = "{{Item Link|Guild Reprint: Malabal Tor Lore|id=120397|quality=f}}",
			["Online:Guild Reprint: Myths of the Mundus"] = "{{Item Link|Guild Reprint: Myths of the Mundus|id=120392|quality=f}}",
			["Online:Guild Reprint: Oblivion Lore"] = "{{Item Link|Guild Reprint: Oblivion Lore|id=120393|quality=f}}",
			["Online:Guild Reprint: Poetry and Song"] = "{{Item Link|Guild Reprint: Poetry and Song|id=120394|quality=f}}",
			["Online:Guild Reprint: Reaper's March Lore"] = "{{Item Link|Guild Reprint: Reaper's March Lore|id=120404|quality=f}}",
			["Online:Guild Reprint: Rivenspire Lore"] = "{{Item Link|Guild Reprint: Rivenspire Lore|id=120379|quality=f}}",
			["Online:Guild Reprint: Shadowfen Lore"] = "{{Item Link|Guild Reprint: Shadowfen Lore|id=120382|quality=f}}",
			["Online:Guild Reprint: Stonefalls Lore"] = "{{Item Link|Guild Reprint: Stonefalls Lore|id=120396|quality=f}}",
			["Online:Guild Reprint: Stormhaven Lore"] = "{{Item Link|Guild Reprint: Stormhaven Lore|id=120378|quality=f}}",
			["Online:Guild Reprint: Tamriel History"] = "{{Item Link|Guild Reprint: Tamriel History|id=120395|quality=f}}",
			["Online:Guild Reprint: The Rift Lore"] = "{{Item Link|Guild Reprint: The Rift Lore|id=120400|quality=f}}",
			["Online:Guild Reprint: The Trial of Eyevea"] = "{{Item Link|Guild Reprint: The Trial of Eyevea|id=120383|quality=s}}"
		};
	}

	protected override string GetEditSummary(Page page) => "Replace deleted page links with Item Link";

	protected override void LoadPages()
	{
		foreach (var from in this.replacements.Keys)
		{
			this.Pages.GetBacklinks(from, BacklinksTypes.Backlinks, true, Filter.Exclude);
		}
	}

	protected override void ParseText(SiteParser parser)
	{
		var sections = parser.ToSections(2);
		foreach (var section in sections)
		{
			Debug.WriteLine(section.GetRawTitle());
		}

		if (sections.FindFirst("Purchase") is not Section purchase)
		{
			return;
		}

		foreach (var (from, to) in this.replacements)
		{
			var index = purchase.Content.FindIndex(node => node is ILinkNode link && link.GetTitle(this.Site) == from);
			if (index != -1)
			{
				if (index > 0 && purchase.Content[index - 1] is ITextNode textBefore)
				{
					textBefore.Text = textBefore.Text.TrimEnd('\'');
				}

				if (index < (purchase.Content.Count - 1) && purchase.Content[index + 1] is ITextNode textAfter)
				{
					textAfter.Text = textAfter.Text.TrimStart('\'');
				}

				purchase.Content.RemoveAt(index);
				purchase.Content.InsertParsed(index, to);
			}
		}

		parser.FromSections(sections);
		parser.UpdatePage();
	}
}