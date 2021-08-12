namespace RobinHood70.HoodBot.Uesp
{
	using System;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	public static class UespFunctions
	{
		public static string IconAbbreviation(SiteTemplateNode template)
		{
			var templateTitle = template.TitleValue;
			if (templateTitle.Namespace != MediaWikiNamespaces.Template ||
				!templateTitle.PageNameEquals("Icon") ||
				template.Find(1) is not IParameterNode iconTypeParam ||
				template.Find(2) is not IParameterNode iconNameParam)
			{
				throw new InvalidOperationException();
			}

			var iconType = iconTypeParam.Value.ToRaw();
			var iconName = iconNameParam.Value.ToRaw();
			var extension = template.Find(3)?.ToString() ?? "png";

			return IconAbbreviation(iconType, iconName, extension);
		}

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