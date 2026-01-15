namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon.Parser;

[method: JobInfo("One-Off Job")]
internal sealed class OneOffJob(JobManager jobManager) : EditJob(jobManager)
{
	#region Static Fields
	private static readonly Dictionary<string, string> ItemCategories = new(StringComparer.OrdinalIgnoreCase)
	{
		["Armor"] = "Armor-Cuirasses",
		["Bikini"] = "Armor-Cuirasses",
		["Blockade"] = "Armor-Shields",
		["Boots"] = "Armor-Boots",
		["Bracers"] = "Armor-Bracers",
		["Breastplate"] = "Armor-Cuirasses",
		["Breath"] = "Armor-Helmets",
		["Companion"] = "Armor-Helmets",
		["Cowl"] = "Armor-Hoods",
		["Cuirass"] = "Armor-Cuirasses",
		["Fist"] = "Armor-Gauntlets",
		["Gauntlets"] = "Armor-Gauntlets",
		["Gloves"] = "Armor-Gauntlets",
		["Greaves"] = "Armor-Greaves",
		["Guardian"] = "Armor-Shields",
		["Helm"] = "Armor-Helmets",
		["Helmet"] = "Armor-Helmets",
		["Hood"] = "Armor-Hoods",
		["Insignia"] = "Armor-Shields",
		["Leggings"] = "Armor-Greaves",
		["Mask"] = "Armor-Hoods",
		["Pace"] = "Armor-Boots",
		["Savior"] = "Armor-Cuirasses",
		["Shield"] = "Armor-Shields",
		["Spellshards"] = "Armor-Greaves",
		["Tunic"] = "Armor-Cuirasses"
	};

	private static readonly HashSet<string> WeaponsPages = new(StringComparer.Ordinal)
	{
		"Ammunition",
		"Magic Weapons",
		"Weapons",
	};

	private static readonly HashSet<string> SkipNames = new(StringComparer.Ordinal)
	{
		"Black and White Akaviri Shield",
		"Exquisite Ceramic Pitcher",
		"Exquisite Planter",
		"Fine Ceramic Pitcher",
		"Fine Planter",
		"Gold Bowl",
		"Gold Goblet",
		"Gold Pitcher",
		"Gold Urn",
		"Hithlain",
		"Imperial Legion Cuirass",
		"Imperial Watch Cuirass",
		"Naith",
		"Noble Dark Steel Rapier",
		"Tarnished Imperial Legion Cuirass",
		"Voice of Nature's Greaves"
	};
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Create redirect for item";

	protected override void LoadPages()
	{
		this.DoWordBasedLists();
		this.DoIngredients();
		this.DoHeaderBasedLists();
		var titles = new TitleCollection(this.Site);
		foreach (var page in this.Pages)
		{
			titles.Add(page.Title);
		}

		var existingPages = new PageCollection(this.Site, PageModules.Info).GetTitles(titles);
		existingPages.RemoveExists(false);
		foreach (var page in existingPages)
		{
			Debug.WriteLine("Page already exists: " + page.Title);
			this.Pages.Remove(page.Title);
		}
	}

	protected override void PageLoaded(Page page) => throw new NotSupportedException();
	#endregion

	#region Private Static Methods
	private static string? GetArmorCategory(string itemName)
	{
		var titleWords = itemName.Split(TextArrays.Space);
		var lastWord = titleWords[^1];
		if (titleWords.Length > 1 && lastWord.StartsWith('(') && lastWord.EndsWith(')'))
		{
			lastWord = titleWords[^2];
		}

		return !ItemCategories.TryGetValue(lastWord, out var retval)
			? throw new InvalidOperationException()
			: retval;
	}
	#endregion

	#region Private Methods
	private void AddPageFromAnchor(Title title, ITemplateNode template)
	{
		if (template.GetValue(1) is not string itemName)
		{
			throw new InvalidOperationException();
		}

		if (SkipNames.Contains(itemName))
		{
			return;
		}

		var subPageName = title.SubPageName();
		var itemCategory =
			subPageName.OrdinalEquals("Ingredients") ? "Ingredients" :
			subPageName.OrdinalEquals("Armor (Light)") ? GetArmorCategory(itemName) :
			subPageName.OrdinalEquals("Armor (Heavy)") ? GetArmorCategory(itemName) :
			WeaponsPages.Contains(subPageName) ? "Weapons-" + template.GetValue("fakeCategory") :
			template.GetValue("fakeCategory");

		var page = this.GetPage(title, itemName, itemCategory) ?? throw new InvalidOperationException("Failed to create page.");
		if (!this.Pages.TryAdd(page))
		{
			Debug.WriteLine("Name conflict: " + page.Title.SubPageName());
		}
	}

	private void DoHeaderBasedLists()
	{
		var pages = new PageCollection(this.Site).GetTitles(
			"Oblivion Mod:Oscuro's Oblivion Overhaul/Ammunition",
			"Oblivion Mod:Oscuro's Oblivion Overhaul/Armor (Misc)",
			"Oblivion Mod:Oscuro's Oblivion Overhaul/Clothing",
			"Oblivion Mod:Oscuro's Oblivion Overhaul/Potions",
			"Oblivion Mod:Oscuro's Oblivion Overhaul/Weapons",
			"Oblivion Mod:Oscuro's Oblivion Overhaul/Miscellaneous Items",
			"Oblivion Mod:Oscuro's Oblivion Overhaul/Magic Weapons");
		foreach (var page in pages)
		{
			var parser = new SiteParser(page);
			var lastCategory = string.Empty;
			foreach (var node in parser)
			{
				if (node is IHeaderNode headerNode)
				{
					lastCategory = headerNode.GetTitle(true)
						.Replace("Magical ", string.Empty, StringComparison.Ordinal)
						.Replace("Non-magical ", string.Empty, StringComparison.Ordinal)
						.Replace("Robes & Gowns", "Robes and Gowns", StringComparison.Ordinal)
						.Replace("Robes/Gowns", "Robes and Gowns", StringComparison.Ordinal);
				}
				else if (node is ITemplateNode template && template.GetTitle(this.Site) == "Template:Anchor")
				{
					template.Add("fakeCategory", lastCategory);
					this.AddPageFromAnchor(page.Title, template);
				}
			}
		}
	}

	private void DoIngredients()
	{
		// This is precisely the same as the word-based lists, but separated out in case it needs adjustment later since the two pages have very different formats.
		var pages = new PageCollection(this.Site).GetTitles("Oblivion Mod:Oscuro's Oblivion Overhaul/Ingredients");
		foreach (var page in pages)
		{
			var parser = new SiteParser(page);
			foreach (var template in parser.FindTemplates("Anchor"))
			{
				this.AddPageFromAnchor(page.Title, template);
			}
		}
	}

	private void DoWordBasedLists()
	{
		var pages = new PageCollection(this.Site).GetTitles(
			"Oblivion Mod:Oscuro's Oblivion Overhaul/Armor (Light)",
			"Oblivion Mod:Oscuro's Oblivion Overhaul/Armor (Heavy)");
		foreach (var page in pages)
		{
			var weight = page.Title.PageName.EndsWith("(Heavy)", StringComparison.Ordinal) ? "Heavy" : "Light";
			var parser = new SiteParser(page);
			foreach (var template in parser.FindTemplates("Anchor"))
			{
				this.AddPageFromAnchor(page.Title, template);
			}
		}
	}

	private Page? GetPage(Title title, string itemName, string? itemCategory)
	{
		if (itemCategory is null)
		{
			return null;
		}

		var pageName =
			itemName.OrdinalEquals("Sun Child") ? "Sun Child (weapon)" :
			itemName.OrdinalEquals("Torne's Blockade") ? "Torne's Blockade (shield)" :
			itemName;
		var newPage = this.Site.CreatePage("Oblivion Mod:Oscuro's Oblivion Overhaul/" + pageName);
		newPage.Text =
			$"#REDIRECT [[{title}#{itemName}]] " +
			"[[Category:Redirects to Broader Subjects]] " +
			$"[[Category:Oblivion Mod-OOO-{itemCategory}]]";

		return newPage;
	}
	#endregion
}