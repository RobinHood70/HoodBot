namespace RobinHood70.HoodBot.Uesp
{
	using System;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	public static class UespFunctions
	{
		public static string IconAbbreviation(string nsId, SiteTemplateNode template)
		{
			var templateTitle = template.TitleValue;
			if (templateTitle.Namespace != MediaWikiNamespaces.Template ||
				!templateTitle.PageNameEquals("Icon") ||
				template.GetValue(1) is not string iconType ||
				template.GetValue(2) is not string iconName)
			{
				throw new InvalidOperationException();
			}

			var extension = template.GetValue(3) ?? "png";

			return IconAbbreviation(nsId, iconType, iconName, extension);
		}

		public static string IconAbbreviation(string nsId, string iconType, string icon) => IconAbbreviation(nsId, iconType, icon, "png");

		public static string IconAbbreviation(string nsId, string iconType, string icon, string extension) =>
			nsId + "-icon-" + IconNameFromAbbreviation(iconType) + icon.NotNull(nameof(icon)) + '.' + extension;

		public static (UespNamespace? Ns, string? Abbr, string? Name, string? Ext) AbbreviationFromIconName(UespNamespaceList nsList, string iconName)
		{
			iconName.ThrowNull(nameof(iconName));
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

				var ns = nsList.FromId(nsNameSplit[0]);
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

		public static string IconNameFromAbbreviation(string iconType) => iconType.NotNull(nameof(iconType)) switch
		{
			"" => string.Empty,
			"a" => "armor-",
			"ach" => "achievement-",
			"b" => "book-",
			"c" => "clothing-",
			"d" => "dish-",
			"e" => "effect-",
			"f" => "food-",
			"fi" => "fish-",
			"furn" => "furniture-",
			"g" => "glyph-",
			"i" => "ingredient-",
			"j" => "jewelry-",
			"m" => "misc-",
			"pm" => "processed material-",
			"poi" => "poison-",
			"pot" => "potion-",
			"q" => "quest-",
			"r" => "runestone-",
			"re" => "reagent-",
			"rm" => "raw material-",
			"sc" => "Scroll-",
			"sk" => "skill-",
			"sm" => "shadowmark-",
			"so" => "solvent-",
			"sp" => "spell-",
			"st" => "stolen-",
			"sty" => "style material-",
			"sy" => "synergy-",
			"t" => "tool-",
			"tr" => "trait material-",
			"w" => "weapon-",
			_ => iconType + '-'
		};
	}
}