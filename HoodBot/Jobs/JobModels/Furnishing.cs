namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Globalization;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;

	#region Internal Enumerations
	internal enum FurnishingType
	{
		None = -1,
		TraditionalFurnishings,
		SpecialFurnishings,
		CollectibleFurnishings,
		SpecialCollectibles
	}
	#endregion

	internal sealed class Furnishing
	{
		#region Static Fields
		private static readonly HashSet<string> AliveCats = new(StringComparer.Ordinal)
			{
				"Amory Assitants",
				"Banking Assistants",
				"Companions",
				"Creatures",
				"Deconstruction Assistants",
				"Houseguests",
				"Merchant Assistants",
				"Mounts",
				"Non-Combat Pets",
				"Statues",
			};

		private static readonly Regex IngredientsFinder = new(@"\|cffffffINGREDIENTS\|r\n(?<ingredients>.+)$", RegexOptions.ExplicitCapture | RegexOptions.Multiline, Globals.DefaultRegexTimeout);
		private static readonly Regex SizeFinder = new(@"This is a (?<size>\w+) house item.", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
		private static readonly HashSet<string> AllSkills = new(StringComparer.Ordinal)
			{
				"Engraver",
				"Metalworking",
				"Potency Improvement",
				"Provisioning",
				"Recipe Improvement",
				"Solvent Proficiency",
				"Tailoring",
				"Woodworking",
			};
		#endregion

		#region Constructors
		public Furnishing(IDataRecord record, Site site, bool collectible)
		{
			this.Collectible = collectible;
			this.Id = collectible ? (long)record["itemId"] : (int)record["itemId"];
			var titleName = (string)record["name"];
			titleName = titleName.TrimEnd(',');
			titleName = site.SanitizePageName(titleName);
			this.Title = TitleFactory.FromUnvalidated(site[UespNamespaces.Online], titleName);
			if (!this.Title.PageNameEquals(titleName))
			{
				this.TitleName = titleName;
			}

			var desc = (string)record["description"];
			var sizeMatch = SizeFinder.Match(desc);
			this.Size = sizeMatch.Success ? sizeMatch.Groups["size"].Value : null;
			desc = desc
				.Replace(" |cFFFFFF", "\n:", StringComparison.Ordinal)
				.Replace("|r", string.Empty, StringComparison.Ordinal)
				.Replace("and and", "{{sic|and and|and}}", StringComparison.Ordinal);
			this.Description = sizeMatch.Index == 0 && sizeMatch.Length == desc.Length
				? null
				: desc;
			var furnCategory = (string)record["furnCategory"];
			this.Behavior = ((string)record["tags"])
				.Replace(",,", ",", StringComparison.Ordinal)
				.Trim(',');
			if (collectible)
			{
				this.FurnishingCategory = furnCategory;
				this.FurnishingSubcategory = (string)record["furnSubCategory"];
				this.NickName = (string)record["nickname"];
			}
			else
			{
				var bindType = (int)record["bindType"];
				this.BindType = bindType switch
				{
					-1 => null,
					0 => string.Empty,
					1 => "Bind on Pickup",
					2 => "Bind on Equip",
					3 => "Backpack Bind on Pickup",
					_ => throw new InvalidOperationException()
				};

				if (!string.IsNullOrEmpty(furnCategory))
				{
					var furnSplit = furnCategory.Split(TextArrays.Colon, 2);
					if (furnSplit.Length > 0)
					{
						this.FurnishingCategory = furnSplit[0];
						this.FurnishingSubcategory = furnSplit[1]
							.Split(TextArrays.Parentheses)[0]
							.TrimEnd();
					}
				}

				var quality = (string)record["quality"];
				this.Quality = int.TryParse(quality, NumberStyles.Integer, site.Culture, out var qualityNum)
					? "nfsel".Substring(qualityNum - 1, 1)
					: quality;
				this.Type = (ItemType)record["type"];
				var abilityDesc = (string)record["abilityDesc"];
				var ingrMatch = IngredientsFinder.Match(abilityDesc);
				if (ingrMatch.Success)
				{
					var ingredientList = ingrMatch.Groups["ingredients"].Value;
					var entries = ingredientList.Split(", ", StringSplitOptions.None);
					foreach (var entry in entries)
					{
						var ingSplit = entry.Split(" (", 2, StringSplitOptions.None);
						var count = ingSplit.Length == 2
							? ingSplit[1]
							: "1";
						var ingredient = ingSplit[0];
						var addAs = $"{ingredient} ({count})";
						if (AllSkills.Contains(ingredient))
						{
							this.Skills.Add(addAs);
						}
						else
						{
							this.Materials.Add(addAs);
						}
					}
				}
			}

			var furnishingLimitType = collectible
				? (FurnishingType)(sbyte)record["furnLimitType"]
				: (FurnishingType)record["furnLimitType"];
			if (furnishingLimitType == FurnishingType.None)
			{
				furnishingLimitType = (
					AliveCats.Contains(this.FurnishingCategory!) ||
					AliveCats.Contains(this.FurnishingSubcategory!))
						? collectible
							? FurnishingType.SpecialCollectibles
							: FurnishingType.SpecialFurnishings
						: collectible
							? FurnishingType.CollectibleFurnishings
							: FurnishingType.TraditionalFurnishings;
			}

			this.FurnishingLimitType = furnishingLimitType;
			var itemLink = (string)record["resultitemLink"];
			this.ResultItemLink = EsoLog.ExtractItemId(itemLink);
		}
		#endregion

		#region Public Static Properties
		public static Dictionary<FurnishingType, string> FurnishingLimitTypes { get; } = new()
		{
			[FurnishingType.None] = string.Empty,
			[FurnishingType.TraditionalFurnishings] = "Traditional Furnishings",
			[FurnishingType.SpecialFurnishings] = "Special Furnishings",
			[FurnishingType.CollectibleFurnishings] = "Collectible Furnishings",
			[FurnishingType.SpecialCollectibles] = "Special Collectibles",
		};

		public static Dictionary<string, string> PageNameExceptions { get; } = new(StringComparer.Ordinal)
		{
			["Dwarven Spider Pet"] = "Dwarven Spider Pet (furnishing)",
			["Frostbane Bear Mount"] = "Frostbane Bear (mount)",
			["Frostbane Bear Pet"] = "Frostbane Bear (pet)",
			["Frostbane Sabre Cat Pet"] = "Frostbane Sabre Cat (pet)",
			["Frostbane Sabre Cat Mount"] = "Frostbane Sabre Cat (mount)",
			["Frostbane Wolf Mount"] = "Frostbane Wolf (mount)",
			["Frostbane Wolf Pet"] = "Frostbane Wolf (pet)",
		};
		#endregion

		#region Public Properties
		public string? Behavior { get; }

		public string? BindType { get; }

		public bool Collectible { get; }

		public string? Description { get; }

		public FurnishingType FurnishingLimitType { get; }

		public string? FurnishingCategory { get; }

		public string? FurnishingSubcategory { get; }

		public long Id { get; }

		public SortedSet<string> Materials { get; } = new(StringComparer.Ordinal);

		public string? NickName { get; }

		public string? Quality { get; }

		public string? ResultItemLink { get; }

		public string? Size { get; }

		public SortedSet<string> Skills { get; } = new(StringComparer.Ordinal);

		public Title Title { get; }

		public string? TitleName { get; }

		public ItemType Type { get; }
		#endregion

		#region Public Static Methods
		public static string IconName(string itemName) => $"ON-icon-furnishing-{itemName.Replace(':', ',')}.png";

		public static string ImageName(string itemName) => $"ON-furnishing-{itemName.Replace(':', ',')}.jpg";
		#endregion

		#region Public Override Methods
		public override string ToString() => $"({this.Id}) {this.TitleName}";
		#endregion
	}
}
