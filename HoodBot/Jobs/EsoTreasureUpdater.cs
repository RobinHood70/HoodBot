namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Design;
using RobinHood70.HoodBot.Jobs.JobModels;
using RobinHood70.HoodBot.Uesp;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

internal sealed class EsoTreasureUpdater : TemplateJob
{
	#region Static Fields
	private static readonly Dictionary<int, List<(string, string)>> Replacements = new()
	{
		[61172] = [("silver", "[[Lore:Silver|silver]]"), ("big moon", "[[Lore:Masser|big moon]]"), ("little moon", "[[Lore:Secunda|little moon]]"), ("Khajiit", "[[Online:Khajiit|Khajiit]]")],
		[61242] = [("Wayrest", "[[ON:Wayrest|Wayrest]]"), ("courtesan", "[[Lore:Prostitution|courtesan]]")],
		[61246] = [("Mournhold", "[[ON:Mournhold|Mournhold]]"), ("courtesan", "[[Lore:Prostitution|courtesan]]")],
		[61554] = [("xanmeer", "[[Lore:Xanmeer|xanmeer]]"), ("Chid-Moska", "[[Online:Chid-Moska Ruins|Chid-Moska]]")],
		[61688] = [("crystals", "[[Lore:crystals|crystals]]"), ("Ansei", "[[Lore:Ansei|Ansei]]")],
		[61778] = [("brass", "[[Lore:Brass|brass]]"), ("senche-tiger", "[[ON:Senche-Tiger|senche-tiger]]")],
		[61794] = [("Rain Disciple", "[[Online:Rain Disciples|Rain Disciple]]"), ("Halcyon Lake", "[[Online:Halcyon Lake|Halcyon Lake]]")],
		[61811] = [("Queen Ayrenn", "[[ON:Queen Ayrenn|Queen Ayrenn]]")],
		[61949] = [("attractive instrument", "[[Lore:Concertina|attractive instrument]]"), ("Hooped Tree Viper", "[[Lore:Snake|Hooped Tree Viper]]")],
		[61975] = [("Glenumbra", "[[Online:Glenumbra|Glenumbra]]"), ("wandering merchants", "[[Online:Travelling Merchants|wandering merchants]]"), ("wyrd sisters", "[[Online:Beldama Wyrd|wyrd sisters]]"), ("Daenia", "[[Online:Daenia|Daenia]]")],
		[61997] = [("silver", "[[Lore:Silver|silver]]")],
		[62032] = [("enchanted", "[[Lore:enchantment|enchanted]]")],
		[62077] = [("Dwemer", "[[ON:Dwemer|Dwemer]]")],
		[62087] = [("Hallin's Stand", "[[ON:Hallin's Stand|Hallin's Stand]]"), ("Satakalaam", "[[ON:Satakalaam|Satakalaam]]")],
		[62145] = [("Crown noble", "[[Lore:Crowns|Crown noble]]"), ("goat", "[[ON:Goat|goat]]")],
		[62157] = [("frog-kabob", "[[Lore:Bosmer Cuisine#Frog-Kebab|frog-kabob]]"), ("iron", "[[Lore:iron|iron]]"), ("Greenwarden Forge", "[[Online:Greenwarden Forge|Greenwarden Forge]]"), ("Elden Root", "[[Online:Elden Root|Elden Root]]")],
		[62168] = [("werewolf", "[[ON:Werewolf (NPC)|werewolf]]"), ("orichalcum", "[[Lore:Orichalcum|orichalcum]]"), ("silver", "[[Lore:Silver|silver]]")],
		[62285] = [("moon-sugar", "[[Lore:Moon Sugar|moon-sugar]]")],
		[62375] = [("Copper", "[[Lore:Copper|Copper]]"), ("Saltrice", "[[ON:Saltrice|Saltrice]]"), ("Sathram", "[[Online:Sathram Plantation|Sathram]]")],
		[62402] = [("silver", "[[Lore:Silver|silver]]"), ("stylized Dragon", "stylized [[ON:Dragon|Dragon]]")],
		[62412] = [("pig-bladder", "[[ON:Pig|pig-bladder]]"), ("Oldgate", "[[Online:Oldgate|Oldgate]]"), ("Meat Hut", "[[Online:The Meat Hut|Meat Hut]]")],
		[62437] = [("senche-tiger", "[[Online:Senche|senche-tiger]]")],
		[62555] = [(" god", "[[Lore:Dibella| god]]"), ("erotic", "[[Lore:Sex|erotic]]"), ("Nord", "[[ON:Nord|Nord]]")],
		[62761] = [("gold", "[[Lore:Gold|gold]]"), ("Chrysamere", "[[Lore:Chrysamere|Chrysamere]]")],
		[62781] = [("gold", "[[Lore:Gold|gold]]"), ("Chrysamere", "[[Lore:Chrysamere|Chrysamere]]")],
		[62880] = [("Anka'Ra", "{{sic|[[Online:Anka-Ra|Anka'Ra]]|Anka-Ra}}")],
		[62995] = [("Khajiiti", "[[ON:Khajiit|Khajiiti]]"), ("monkey", "[[ON:Monkey|monkey]]")],
		[63126] = [("aetherium", "[[Lore:Aetherium|aetherium]]")],
		[63389] = [("Precious stone", "[[Lore:Pearl|Precious stone]]"), ("Coral Kingdoms", "[[Lore:Coral Kingdoms|Coral Kingdoms]]"), ("man or mer", "[[Lore:Man|man]] or [[Lore:Mer|mer]]"), ("First Era", "[[Lore:First Era|First Era]]")],
		[64300] = [("dwarven", "[[Lore:Dwemer|dwarven]]"), ("Numidium.", "[[Lore:Numidium|Numidium]].<br><small>(Originally named '''{{ESO Quality Color|e|Bolt of the Second Numidium}}''' before [[Online:Patch/2.0.1|Patch 2.0.1]], with the following description: A massive dwarven bolt, unnaturally warm to the touch. Scholarly markings link it to the legendary Brass God, [[Lore:Akulakhan|Akulakhan]].)</small>")],
		[64416] = [("pewter", "[[Lore:pewter|pewter]]"), ("blue star", "[[Lore:Zenithar|blue star]]")],
		[73773] = [("Golden Beast", "[[Lore:Darloc Brae|Golden Beast]]"), ("Kari's Hit List", "[[Online:Kari's Hit List|Kari's Hit List]]"), ("Thieves Den", "[[Online:Thieves Den|Thieves Den]]"), ("Master Thief", "[[Online:A Cutpurse Above|Master Thief]]")],
		[73798] = [("senche-tiger", "[[Online:Senche|senche-tiger]]")],
		[73800] = [("fortune-telling", "[[Lore:Divination|fortune-telling]]")],
		[79580] = [("pirate", "[[Lore:Piracy|pirate]]"), ("Akaviri Potentate", "[[Lore:Akaviri Potentate|Akaviri Potentate]]")],
		[79591] = [("Karthman Red-Sails", "[[Online:Red Sails|Karthman Red-Sails]]")],
		[79626] = [("brass", "[[Lore:Brass|brass]]"), ("Khajitti", "[[ON:Khajiit|{{Sic|Khajiiti|Khajiiti|nolink=1}}]]")],
		[79652] = [("Longhouse", "[[Lore:Longhouse Emperors|Longhouse]]"), ("Varen's Rebellion", "[[Lore:Varen's Rebellion|Varen's Rebellion]]")],
		[126184] = [("mudcrabs", "[[Online:Mudcrab|mudcrabs]]")],
		[126324] = [("daedra", "[[ON:Daedra|daedra]]"), ("ectoplasm", "[[ON:Ectoplasm|ectoplasm]]")],
		[138854] = [("Alinor", "[[ON:Alinor|Alinor]]")],
		[138855] = [("obsidian", "[[Online:Obsidian|obsidian]]"), ("Nocturnal", "[[Online:Nocturnal|Nocturnal]]")],
		[138860] = [("swan", "[[Lore:swan|swan]] ")],
		[138878] = [("coral", "[[Lore:coral|coral]]")],
		[138927] = [("Psijic", "[[ON:Psijic Order|Psijic]]"), ("Mannimarco", "[[ON:Mannimarco|Mannimarco]]")],
		[138931] = [("eagle", "[[Lore:Eagle|eagle]]"), ("Auri-El", "[[Lore:Auri-El|Auri-El]]")],
		[138936] = [("Thrassian squid", "[[Lore:Squid|Thrassian squid]]")],
		[145623] = [("Dead-Water tribe", "[[ON:Dead-Water Tribe|Dead-Water tribe]]")],
		[145738] = [("eelskin", "[[Lore:Eel|eelskin]]"), ("fortune-telling", "[[Lore:Divination|fortune-telling]]")],
		[145757] = [("hatchling", "[[ON:Argonian|hatchling]]")],
		[150526] = [("Khajiit", "[[ON:Khajiit|Khajiit]]"), ("earlier era", "[[Lore:First Era|earlier era]]")],
		[150533] = [("Senche-raht", "[[ON:Senche-raht|Senche-raht]]")],
		[150536] = [(" cat ", " [[Online:cat|cat]] "), ("Khajiiti", "[[Lore:Khajiit|Khajiiti]]"), ("deity", "[[Lore:Alkosh|deity]]")],
		[150548] = [("duneripper-hide", "[[ON:Duneripper|duneripper-hide]]")],
		[150553] = [("Elsweyr", "[[ON:Elsweyr|Elsweyr]]")],
		[150591] = [("'What", "<nowiki>'What"), ("price!'", "price!'</nowiki>")],
		[166534] = [("Atmoran", "[[Lore:Atmoran|Atmoran]]"), ("wolf", "[[Online:wolf|wolf]]")],
		[166544] = [("Karthwatch", "[[ON:Karthwatch|Karthwatch]]"), ("mammoth", "[[ON:Mammoth|mammoth]]")],
		[166588] = [("rose", "[[Lore:rose|rose]]"), ("vampire", "[[Online:Vampire|vampire]]")],
		[166594] = [("bleeding crown", "[[Lore:Bleeding Crown|bleeding crown]]")],
		[166614] = [("tundra cotton", "[[Lore:Tundra Cotton|tundra cotton]]"), ("slaughterfish", "[[Online:Slaughterfish|slaughterfish]]")],
		[166627] = [("horker", "[[Online:Horker|horker]]")],
		[166633] = [("meaderies", "[[Lore:Mead|meaderies]]"), ("Skyrim", "[[Online:Provinces#Skyrim|Skyrim]]")],
		[171239] = [("bronze", "[[Lore:bronze|bronze]]"), ("Dwarven", "[[ON:Dwemer|Dwarven]]")],
		[177360] = [("ancestor moth", "[[Lore:Ancestor Moth|ancestor moth]]"), ("Nibenay", "[[Lore:Nibenay|Nibenay]]"), ("Empire", "[[Lore:Empire|Empire]]")],
		[177373] = [("Haj mota", "[[Online:Haj Mota|Haj mota]]")],
		[177388] = [("werewolf", "[[Online:Werewolf|werewolf]]")],
		[183054] = [("lycanthropy", "[[Online:Werewolf|lycanthropy]]"), ("Hircine's", "[[Online:Hircine|Hircine's]]")],
		[183120] = [("Dune", "[[Online:Dune|Dune]]")],
		[183185] = [("torchbug", "[[Online:torchbug|torchbug]]")],
		[191119] = [("Faun's", "[[Online:Faun|Faun's]]")],
		[198186] = [("Dark Elf", "[[Online:Dark Elf|Dark Elf]]"), ("fungi", "[[Lore:fungi|fungi]]")],
		[198262] = [("Kwama", "[[Online:Kwama|Kwama]]"), ("Morrowind", "[[Online:Morrowind|Morrowind]]"), ("black lichen", "[[Lore:Black Lichen|black lichen]]")],
	};
	#endregion

	#region Fields
	private readonly FlatteningComparer comparer;
	private readonly Dictionary<int, Treasure> items = [];
	private readonly SortedSet<int> unused = [];
	#endregion

	#region Constructors
	[JobInfo("Update Treasures", "ESO")]
	public EsoTreasureUpdater(JobManager jobManager)
		: base(jobManager)
	{
		var context = new Context(this.Site);
		var subComparer = this.Site.GetStringComparer(false);
		this.comparer = new FlatteningComparer(context, subComparer);
	}

	protected override string TemplateName => "ESO Treasure Entry";
	#endregion

	#region Private Properties
	private IReadOnlyDictionary<string, Title> Files => field ??= EsoFiles.GetOriginalFiles((UespSite)this.Site);

	private UespNamespaceList NsList => field ??= new UespNamespaceList(this.Site);
	#endregion

	#region Protected Override Methods
	protected override void AfterLoadPages()
	{
		if (this.items.Count == 0)
		{
			return;
		}

		this.WriteLine("== Unused Items ==");
		this.WriteLine("{|class=\"wikitable sortable\"");
		this.WriteLine("!class=unsortable| !!Item!!Location(s)!!Quest!!Description");
		foreach (var id in this.unused)
		{
			var item = this.items[id];
			var abbr = string.Empty;
			var name = string.Empty;
			if (this.Files.TryGetValue(item.Icon, out var fileTitle))
			{
				var (_, abbr2, name2, _) = UespFunctions.AbbreviationFromIconName(this.NsList, fileTitle.PageName);
				abbr = abbr2 ?? string.Empty;
				name = name2 ?? string.Empty;
			}

			this.WriteLine($"{{{{{this.TemplateName}|{item.Name}|icontype={abbr}|icon={name}|id={id}|loc=|{item.Description}}}}}");
		}

		this.WriteLine("|}");
	}

	protected override void BeforeLoadPages()
	{
		foreach (var row in Database.RunQuery(EsoLog.Connection, "SELECT itemId, icon, name, description, tags, quality, value FROM minedItemSummary WHERE type IN (56, 57) AND description != ''"))
		{
			var id = (int)row["itemId"];
			var icon = (string)row["icon"];
			var name = (string)row["name"];
			var description = (string)row["description"];
			var tags = (string)row["tags"];
			var tagSplit = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			tags = tagSplit.Length == 0 ? "None" : string.Join(", ", tagSplit);
			var quality = (string)row["quality"];
			var value = (string)row["value"];

			description = description.Replace("  ", " ", StringComparison.Ordinal);
			if (Replacements.GetValueOrDefault(id) is List<(string, string)> replacementList)
			{
				foreach (var (oldValue, newValue) in replacementList)
				{
					description = description.Replace(oldValue, newValue, StringComparison.Ordinal);
				}
			}

			this.items.Add(id, new Treasure(icon, name, description, tags, quality, value));
			this.unused.Add(id);
		}
	}

	protected override string GetEditSummary(Page page) => "Update quest item info";

	protected override void LoadPages() => this.Pages.GetBacklinks($"Template:{this.TemplateName}", BacklinksTypes.EmbeddedIn, true, Filter.Exclude);

	protected override void ParseTemplate(ITemplateNode template, SiteParser parser)
	{
		// Skip if id parameter is missing, invalid, or doesn't match an item in the dictionary.
		if (template.PrioritizedFind("itemId", "id") is not IParameterNode idParam)
		{
			Debug.WriteLine($"ID parameter is missing or invalid on page {parser.Title} for template: " + template.ToRaw());
			return;
		}

		var idValue = idParam.GetValue();
		if (!int.TryParse(idValue, this.Site.Culture, out var id) ||
			!this.items.TryGetValue(id, out var item))
		{
			Debug.WriteLine($"ID parameter is invalid or not found in items dictionary on page {parser.Title}: " + idValue);
			return;
		}

		this.unused.Remove(id);
		if (this.Files.TryGetValue(item.Icon, out var fileTitle))
		{
			template.Update("1", $"[[{fileTitle}|48px]]");
		}

		template.Update("2", item.Name, ParameterFormat.Copy, this.comparer);
		template.Update("3", item.Description, ParameterFormat.Copy, this.comparer);
		template.Update("4", item.Tags, ParameterFormat.Copy, this.comparer);

		var qualityName = EsoSpace.GetQualityName(item.Quality) ?? string.Empty;
		var qualityInitial = qualityName.Length > 0 ? char.ToLower(qualityName[0], this.Site.Culture).ToString() : string.Empty;
		var qualityParam = template.Find("5");
		if (qualityParam is null)
		{
			template.Add("5", qualityInitial, ParameterFormat.Copy);
		}
		else if (!qualityInitial.OrdinalEquals("a"))
		{
			var qualityValue = qualityParam.GetValue();
			if (!qualityValue.OrdinalEquals(qualityName) && !qualityValue.OrdinalEquals(item.Quality))
			{
				qualityParam.SetValueNoEscape(qualityInitial, ParameterFormat.Copy);
			}
		}
	}
	#endregion

	#region Private Classes
	private sealed record Treasure(string Icon, string Name, string Description, string Tags, string Quality, string Value);
	#endregion
}