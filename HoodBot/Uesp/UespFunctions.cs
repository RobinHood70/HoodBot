namespace RobinHood70.HoodBot.Uesp;

using System;
using System.Collections.Generic;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.WikiCommon.Parser;

public static class UespFunctions
{
	private static readonly Dictionary<string, string> IconAbbreviations = new(StringComparer.Ordinal)
	{
		[string.Empty] = string.Empty,
		["a"] = "armor-",
		["ach"] = "achievement-",
		["b"] = "book-",
		["c"] = "clothing-",
		["d"] = "dish-",
		["e"] = "effect-",
		["f"] = "food-",
		["fi"] = "fish-",
		["furn"] = "furniture-",
		["g"] = "glyph-",
		["i"] = "ingredient-",
		["j"] = "jewelry-",
		["m"] = "misc-",
		["pm"] = "processed material-",
		["poi"] = "poison-",
		["pot"] = "potion-",
		["q"] = "quest-",
		["r"] = "runestone-",
		["re"] = "reagent-",
		["rm"] = "raw material-",
		["sc"] = "Scroll-",
		["sk"] = "skill-",
		["sm"] = "shadowmark-",
		["so"] = "solvent-",
		["sp"] = "spell-",
		["st"] = "stolen-",
		["sty"] = "style material-",
		["sy"] = "synergy-",
		["t"] = "tool-",
		["tr"] = "trait material-",
		["w"] = "weapon-",
	};

	public static string[] LoreNames { get; } =
	[
		"Names",
		"Altmer Names",
		"Argonian Names",
		"Bosmer Names",
		"Breton Names",
		"Daedra Names",
		"Dunmer Names",
		"Imperial Names",
		"Khajiit Names",
		"Nord Names",
		"Orc Names",
		"Reachman Names",
		"Redguard Names"
	];

	public static string IconAbbreviation(UespNamespace ns, ITemplateNode template)
	{
		ArgumentNullException.ThrowIfNull(ns);
		ArgumentNullException.ThrowIfNull(template);
		return
			template.GetTitle(ns.Site) == TitleFactory.FromTemplate(ns.Site, "Icon") &&
			template.GetValue(1) is string iconType &&
			template.GetValue(2) is string iconName
				? IconAbbreviation(ns, iconType, iconName, template.GetValue(4) ?? "png")
				: throw new InvalidOperationException();
	}

	public static string IconAbbreviation(UespNamespace ns, string iconType, string icon) => IconAbbreviation(ns, iconType, icon, "png");

	public static string IconAbbreviation(UespNamespace ns, string iconType, string icon, string extension)
	{
		ArgumentNullException.ThrowIfNull(ns);
		ArgumentNullException.ThrowIfNull(iconType);
		ArgumentNullException.ThrowIfNull(icon);
		ArgumentNullException.ThrowIfNull(extension);
		return ns.Id + "-icon-" + IconNameFromAbbreviation(iconType) + icon + '.' + extension;
	}

	public static (UespNamespace? Ns, string? Abbr, string? Name, string? Ext) AbbreviationFromIconName(UespNamespaceList nsList, string iconName)
	{
		ArgumentNullException.ThrowIfNull(iconName);
		ArgumentNullException.ThrowIfNull(nsList);
		var nsNameSplit = iconName.Split("-icon-", 2);
		if (nsNameSplit.Length == 2)
		{
			var name = nsNameSplit[1];
			var extOffset = name.LastIndexOf('.');
			string? ext;
			if (extOffset == -1)
			{
				ext = null;
			}
			else
			{
				ext = name[(extOffset + 1)..];
				name = name[..extOffset];
			}

			var ns = nsList[nsNameSplit[0]];
			var abbrNameSplit = name.Split('-', 2);
			var abbr = abbrNameSplit[0] switch
			{
				"" => string.Empty,
				"armor" => "a",
				"achievement" => "ach",
				"book" => "b",
				"clothing" => "c",
				"dish" => "d",
				"effect" => "e",
				"food" => "f",
				"fish" => "fi",
				"furniture" => "furn",
				"glyph" => "g",
				"ingredient" => "i",
				"jewelry" => "j",
				"misc" => "m",
				"processed material" => "pm",
				"poison" => "poi",
				"potion" => "pot",
				"quest" => "q",
				"runestone" => "r",
				"reagent" => "re",
				"raw material" => "rm",
				"Scroll" => "sc",
				"skill" => "sk",
				"shadowmark" => "sm",
				"solvent" => "so",
				"spell" => "sp",
				"stolen" => "st",
				"style material" => "sty",
				"synergy" => "sy",
				"tool" => "t",
				"trait material" => "tr",
				"weapon" => "w",
				_ => abbrNameSplit[0]
			};

			var iconShortName = abbrNameSplit.Length > 1 ? abbrNameSplit[1] : string.Empty;

			return (ns, abbr, iconShortName, ext);
		}

		return (null, null, null, null);
	}

	public static string IconNameFromAbbreviation(string iconType)
	{
		ArgumentNullException.ThrowIfNull(iconType);
		if (!IconAbbreviations.TryGetValue(iconType, out var retval))
		{
			retval = iconType + '-';
		}

		return retval;
	}
}