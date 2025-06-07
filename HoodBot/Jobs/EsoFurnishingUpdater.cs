namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Design;
using RobinHood70.HoodBot.Jobs.Design;
using RobinHood70.HoodBot.Jobs.JobModels;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

#region Internal Enumerations
internal enum ItemType
{
	Container = 18,
	Recipes = 29,
	Furnishing = 61,
}
#endregion

internal sealed class EsoFurnishingUpdater : TemplateJob
{
	#region Private Constants
	private const string CollectiblesQuery = $"SELECT convert(cast(convert(description using latin1) as binary) using utf8) description, furnCategory, furnLimitType, furnSubCategory, id itemId, itemLink resultitemLink, name, nickname, tags FROM collectibles WHERE furnCategory != ''";
	#endregion

	#region Static Fields
	private static readonly HashSet<long> IgnoreIds = [194537];
	private static readonly string MinedItemsQuery = $"SELECT abilityDesc, bindType, convert(cast(convert(description using latin1) as binary) using utf8) description, furnCategory, furnLimitType, itemId, name, quality, resultitemLink, tags, type FROM uesp_esolog.minedItemSummary WHERE type IN({(int)ItemType.Container}, {(int)ItemType.Recipes}, {(int)ItemType.Furnishing})";
	#endregion

	#region Fields
	private readonly Dictionary<long, Furnishing> collectibles = [];
	private readonly Dictionary<long, Furnishing> furnishings = [];
	private readonly List<string> fileMessages = [];
	private readonly List<string> pageMessages = [];
	private readonly Dictionary<string, long> nameLookup = new(StringComparer.Ordinal);

	//// private readonly Dictionary<Title, Furnishing> furnishingDictionary = new();
	#endregion

	#region Constructors
	[JobInfo("Furnishings", "ESO Update")]
	public EsoFurnishingUpdater(JobManager jobManager)
		: base(jobManager)
	{
		//// jobManager.ShowDiffs = false;
		if (this.Results is PageResultHandler pageResults)
		{
			var title = pageResults.Title;
			pageResults.Title = TitleFactory.FromValidated(title.Namespace, title.PageName + "/ESO Furnishings");
			pageResults.SaveAsBot = false;
		}
	}
	#endregion

	#region Public Override Properties
	public override string LogName { get; } = "ESO Furnishing Update";
	#endregion

	#region Protected Override Properties
	protected override string TemplateName { get; } = "Online Furnishing Summary";
	#endregion

	#region Protected Override Methods
	protected override void AfterLoadPages()
	{
		if (this.pageMessages.Count == 0 && this.fileMessages.Count == 0)
		{
			return;
		}

		this.WriteLine("__FORCETOC__");
		if (this.pageMessages.Count > 0)
		{
			this.WriteLine("== Online Page Name Issues ==");
			this.pageMessages.Sort(StringComparer.Ordinal);
			foreach (var message in this.pageMessages)
			{
				this.WriteLine(message);
				this.WriteLine(string.Empty);
			}
		}

		if (this.fileMessages.Count > 0)
		{
			this.WriteLine("== File Page Name Issues ==");
			this.fileMessages.Sort(StringComparer.Ordinal);
			foreach (var message in this.fileMessages)
			{
				this.WriteLine(message);
				this.WriteLine(string.Empty);
			}
		}
	}

	protected override void BeforeLoadPages()
	{
		/*
		TitleCollection furnishingFiles = new(this.Site);
		furnishingFiles.GetNamespace(MediaWikiNamespaces.File, CommonCode.Filter.Any, "ON-furnishing-");
		furnishingFiles.GetNamespace(MediaWikiNamespaces.File, CommonCode.Filter.Any, "ON-item-furnishing-");
		*/

		foreach (var furnishing in Database.RunQuery(EsoLog.Connection, CollectiblesQuery, record => new Furnishing(record, this.Site, true)))
		{
			this.collectibles.Add(furnishing.Id, furnishing);
		}

		foreach (var furnishing in Database.RunQuery(EsoLog.Connection, MinedItemsQuery, record => new Furnishing(record, this.Site, false)))
		{
			this.furnishings.Add(furnishing.Id, furnishing);
		}

		var dupes = new HashSet<string>(StringComparer.Ordinal);
		this.FindDupes(this.collectibles, dupes);
		this.FindDupes(this.furnishings, dupes);
	}

	protected override string GetEditSummary(Page page) => "Update info from ESO database";

	protected override void ParseTemplate(ITemplateNode template, SiteParser parser)
	{
		ArgumentNullException.ThrowIfNull(parser);
		if (this.GenericTemplateFixes(template))
		{
			this.Warn("Template has anonymous parameter on " + parser.Title.FullPageName());
		}

		this.FurnishingFixes(template, parser.Page);
	}
	#endregion

	#region Private Static Methods
	private static void CheckBehavior(ITemplateNode template, Furnishing furnishing)
	{
		if (furnishing.Behavior is not null && furnishing.Behavior.Length > 0)
		{
			var behavior = EsoSpace.TrimBehavior(template.GetValue("behavior"));
			if (behavior is null || behavior.Length == 0)
			{
				template.Remove("behavior");
			}
			else
			{
				template.AddIfNotExists("behavior", furnishing.Behavior, ParameterFormat.OnePerLine);
			}
		}
	}

	private static void CheckIcon(ITemplateNode template, string labelName)
	{
		var fileName = labelName.Replace(':', ',');
		if (template.GetValue("icon").OrdinalEquals($"ON-icon-furnishing-{fileName}.png"))
		{
			template.Remove("icon");
		}
	}

	private static string CheckName(ITemplateNode template, string labelName)
	{
		if (template.GetValue("name") is string nameValue)
		{
			if (!nameValue.OrdinalEquals(labelName))
			{
				return nameValue;
			}

			template.Remove("name");
		}

		return labelName;
	}

	private static void FixBehavior(ITemplateNode template)
	{
		if (template.Find("behavior") is IParameterNode behavior)
		{
			var list = new List<string>(behavior.GetValue().Split(TextArrays.Comma));
			for (var i = list.Count - 1; i >= 0; i--)
			{
				list[i] = list[i].Trim();
				if (list[i].Length == 0)
				{
					list.RemoveAt(i);
				}
				else if (list[i].StartsWith("Light ", StringComparison.OrdinalIgnoreCase))
				{
					list[i] = "Light";
				}
			}

			behavior.SetValue(string.Join(", ", list), ParameterFormat.OnePerLine);
		}
	}
	#endregion

	#region Private Methods
	private void CheckImage(ITemplateNode template, string name, string link)
	{
		var fileSpace = this.Site[MediaWikiNamespaces.File];
		var imageName = Furnishing.ImageName(name);
		if (template.GetValue("image") is string imageValue)
		{
			imageValue = imageValue.Trim();
			if (imageValue.Length != 0 &&
				!imageValue.OrdinalEquals(imageName))
			{
				imageName = imageValue;
			}
			else
			{
				template.Remove("image");
			}
		}

		var nameFix = imageName.Replace(':', ',');
		var oldTitle = TitleFactory.FromUnvalidated(fileSpace, imageName).Title;
		var newTitle = TitleFactory.FromUnvalidated(fileSpace, nameFix).Title;

		if (!oldTitle.LabelName().OrdinalEquals(newTitle.LabelName()))
		{
			this.fileMessages.Add($"{SiteLink.ToText(oldTitle, LinkFormat.LabelName)} on {link} ''should be''<br>\n{newTitle.PageName}");

			var noItem1 = oldTitle.PageName.Replace("-item-", "-", StringComparison.Ordinal);
			var noItem2 = newTitle.PageName.Replace("-item-", "-", StringComparison.Ordinal);
			if ((oldTitle.PageName.Contains("-item-", StringComparison.Ordinal) ||
				newTitle.PageName.Contains("-item-", StringComparison.Ordinal)) &&
				noItem1.OrdinalEquals(noItem2))
			{
				Debug.WriteLine($"File Replace Needed:\n  {oldTitle.FullPageName()} with\n  {newTitle.FullPageName()}");
			}
		}
	}

	private void CheckTitle(Title title, string labelName, Furnishing furnishing)
	{
		var compareName = Furnishing.PageNameExceptions.GetValueOrDefault(labelName, furnishing.Title.LabelName());
		if (!labelName.OrdinalEquals(compareName))
		{
			this.pageMessages.Add($"[[{title.FullPageName()}|{labelName}]] ''should be''<br>\n" +
			  $"{compareName}");
			if (!title.PageName.Contains(':', StringComparison.Ordinal) &&
				compareName.Contains(':', StringComparison.Ordinal) &&
				title.PageName.Replace(',', ':').OrdinalEquals(furnishing.Title.PageName))
			{
				Debug.WriteLine($"Page Replace Needed: {title.FullPageName()}\t{furnishing.Title}");
			}
		}
	}

	private void FindDupes(Dictionary<long, Furnishing> items, HashSet<string> dupes)
	{
		foreach (var item in items)
		{
			var furnishing = item.Value;
			var labelName = furnishing.Title.LabelName();
			if (!dupes.Contains(labelName) && !this.nameLookup.TryAdd(labelName, item.Key))
			{
				dupes.Add(labelName);
				this.nameLookup.Remove(labelName);
			}
		}
	}

	private Furnishing? FindFurnishing(ITemplateNode template, Page page, string labelName)
	{
		Furnishing? retval = null;
		if (long.TryParse(template.GetValue("id"), NumberStyles.None, page.Site.Culture, out var id))
		{
			if (IgnoreIds.Contains(id))
			{
				return null;
			}

			if (!this.furnishings.TryGetValue(id, out retval) && !this.collectibles.TryGetValue(id, out retval))
			{
				Debug.WriteLine($"Furnishing ID {id} not found on page {SiteLink.ToText(page)}.");
			}
		}
		else
		{
			Debug.WriteLine($"Furnishing ID on {SiteLink.ToText(page)} is missing or nonsensical.");
		}

		if (retval is null && this.nameLookup.TryGetValue(labelName, out var recoveredId))
		{
			Debug.WriteLine($"  Recovered ID {recoveredId} from {labelName}.");
			if (this.collectibles.TryGetValue(recoveredId, out retval) || this.furnishings.TryGetValue(recoveredId, out retval))
			{
				template.Update("id", recoveredId.ToStringInvariant());
			}
		}

		return retval;
	}

	private void FixBundles(ITemplateNode template)
	{
		if (template.Find("bundles") is IParameterNode bundles)
		{
			var value = bundles.Value;
			var factory = template.Factory;
			for (var i = 0; i < value.Count; i++)
			{
				if (value is ILinkNode link)
				{
					var siteLink = SiteLink.FromLinkNode(this.Site, link);
					value.RemoveAt(i);
					if (siteLink.Text is string text)
					{
						value.Insert(i, factory.TextNode(text));
					}
				}
			}
		}
	}

	private void FixList(ITemplateNode template, string parameterName)
	{
		var plural = parameterName + "s";
		if (template.Find(plural, parameterName) is IParameterNode param)
		{
			param.SetName(plural);
			var curText = param.GetValue();
			var splitOn = curText.Contains('~', StringComparison.Ordinal) ? '~' : ',';
			var split = curText.Split(splitOn, StringSplitOptions.None);
			var list = new List<(string Name, int Value)>(split.Length / 2);
			for (var i = 0; i < split.Length; i += 2)
			{
				split[i + 1] = split[i + 1]
					.Replace(" ", string.Empty, StringComparison.Ordinal)
					.Replace("(", string.Empty, StringComparison.Ordinal)
					.Replace(")", string.Empty, StringComparison.Ordinal);
				var intValue = split[i + 1].Length == 0 ? 1 : int.Parse(split[i + 1], this.Site.Culture);
				list.Add((split[i], intValue));
			}

			if (parameterName.OrdinalEquals("material"))
			{
				list.Sort((item1, item2) =>
					item2.Value.CompareTo(item1.Value) is int result && result == 0
						? string.Compare(item1.Name, item2.Name, false, this.Site.Culture)
						: result);
			}

			var sb = new StringBuilder(list.Count * 10);
			foreach (var (name, value) in list)
			{
				sb
					.Append(name)
					.Append('~')
					.Append(value.ToStringInvariant())
					.Append('~');
			}

			if (sb.Length > 0)
			{
				sb.Remove(sb.Length - 1, 1);
			}

			param.SetValue(sb.ToString(), ParameterFormat.OnePerLine);
		}
	}

	private void FurnishingFixes(ITemplateNode template, Page? page)
	{
		ArgumentNullException.ThrowIfNull(page);
		var labelName = page.Title.LabelName();
		var name = CheckName(template, labelName);
		CheckIcon(template, labelName);
		if (this.FindFurnishing(template, page, labelName) is not Furnishing furnishing)
		{
			return;
		}

		var wikiTitle = page.Title.LabelName();
		if (!furnishing.Title.PageNameEquals(wikiTitle))
		{
			Debug.WriteLine($"Page title \"{wikiTitle}\" != furnishing title \"{furnishing.Title.PageName}\". Check for invalid ID and verify title case in game.");
		}

		this.CheckImage(template, name, SiteLink.ToText(page, LinkFormat.LabelName));
		this.CheckTitle(page.Title, labelName, furnishing);

		template.Update("titlename", furnishing.TitleName, ParameterFormat.OnePerLine, true);
		if (furnishing.Collectible)
		{
			template.Update("nickname", furnishing.NickName, ParameterFormat.OnePerLine, true);
		}

		template.Update("quality", furnishing.Quality, ParameterFormat.OnePerLine, true);

		if (furnishing.Size is not null)
		{
			template.Update("size", furnishing.Size, ParameterFormat.OnePerLine, false);
		}

		template.Update("desc", furnishing.Description, ParameterFormat.OnePerLine, false);
		if (!string.IsNullOrEmpty(furnishing.FurnishingCategory))
		{
			template.Update("cat", furnishing.FurnishingCategory, ParameterFormat.OnePerLine, false);
		}

		if (!string.IsNullOrEmpty(furnishing.FurnishingSubcategory))
		{
			template.Update("subcat", furnishing.FurnishingSubcategory, ParameterFormat.OnePerLine, false);
		}

		CheckBehavior(template, furnishing);

		var craft = template.GetValue("craft");
		if (craft is not null)
		{
			var craftWord = craft switch
			{
				"Alchemy" => "Formula",
				"Blacksmithing" => "Diagram",
				"Clothing" => "Pattern",
				"Enchanting" => "Praxis",
				"Jewelry Crafting" => "Sketch",
				"Provisioning" => "Design",
				"Woodworking" => "Blueprint",
				_ => throw new InvalidOperationException()
			};

			var expectedNmae = craftWord + ": " + (template.GetValue("name") ?? page.Title.LabelName());
			var planname = template.GetValue("planname");
			if (expectedNmae.OrdinalEquals(planname))
			{
				template.Remove("planname");
			}
		}

		if (template.GetValue("planquality") is string planquality && template.GetValue("quality").OrdinalEquals(planquality))
		{
			template.Remove("planquality");
		}

		if (furnishing.Materials.Count > 0)
		{
			template.Update("materials", string.Join('~', furnishing.Materials), ParameterFormat.OnePerLine, true);
		}

		if (furnishing.Skills.Count > 0)
		{
			template.Update("skills", string.Join('~', furnishing.Skills), ParameterFormat.OnePerLine, true);
		}

		var bindTypeValue = template.GetValue("bindtype");
		var bindType = (furnishing.Collectible ||
			bindTypeValue.OrdinalEquals("0"))
				? null
				: furnishing.BindType;
		if (bindType is not null)
		{
			template.Update("bindtype", bindType, ParameterFormat.OnePerLine, true);
		}

		if (furnishing.FurnishingLimitType == FurnishingType.None && string.IsNullOrEmpty(furnishing.Behavior))
		{
			template.Remove("collectible");
		}
		else if (template.GetValue("furnLimitType") is string furnLimitType)
		{
			var wantsToBe = Furnishing.FurnishingLimitTypes[furnishing.FurnishingLimitType];
			if (!(furnLimitType + 's').OrdinalEquals(wantsToBe))
			{
				template.Update("furnLimitType", wantsToBe);
			}

			var showCollectible = furnishing.FurnishingLimitType switch
			{
				FurnishingType.TraditionalFurnishings => furnishing.Collectible,
				FurnishingType.SpecialFurnishings => furnishing.Collectible,
				FurnishingType.CollectibleFurnishings => !furnishing.Collectible,
				FurnishingType.SpecialCollectibles => !furnishing.Collectible,
				FurnishingType.None => throw new InvalidOperationException(),
				_ => throw new InvalidOperationException()
			};

			if (showCollectible)
			{
				template.Update("collectible", furnishing.Collectible ? "1" : "0");
			}
		}
	}

	private bool GenericTemplateFixes(ITemplateNode template)
	{
		template.Remove("animated");
		template.Remove("audible");
		template.Remove("collectible");
		template.Remove("creature");
		template.Remove("houses");
		template.Remove("interactable");
		template.Remove("light");
		template.Remove("lightcolor");
		template.Remove("lightcolour");
		template.Remove("luxury");
		template.Remove("master");
		template.Remove("readable");
		template.Remove("sittable");
		template.Remove("visualfx");

		template.RenameParameter("other", "source");
		template.RenameParameter("recipeid", "planid");
		template.RenameParameter("recipename", "planname");
		template.RenameParameter("recipequality", "planquality");
		template.RenameParameter("style", "theme");
		template.RenameParameter("tags", "behavior");
		template.RenameParameter("type", "furnLimitType");
		template.RenameParameter("description", "desc");
		template.RemoveDuplicates();

		FixBehavior(template);
		this.FixBundles(template);
		this.FixList(template, "material");
		this.FixList(template, "skill");

		return template.Find(1) is not null;
	}
	#endregion
}