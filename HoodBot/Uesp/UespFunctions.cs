﻿namespace RobinHood70.HoodBot.Uesp
{
	using RobinHood70.CommonCode;

	public static class UespFunctions
	{
		public static string IconAbbreviation(string iconType, string icon) => IconAbbreviation(iconType, icon, "png");

		public static string IconAbbreviation(string iconType, string icon, string extension)
		{
			icon.ThrowNull(nameof(icon));
			var unabbreviated = iconType.NotNull(nameof(iconType)) switch
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

			return "ON-icon-" + unabbreviated + icon + '.' + extension;
		}
	}
}