namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Design;
using RobinHood70.HoodBot.Jobs.JobModels;
using RobinHood70.HoodBot.Uesp;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

internal sealed class EsoUpdateQuestItems : TemplateJob
{
	#region Static Fields
	private static readonly Dictionary<int, List<(string, string)>> Replacements = new()
	{
		[6582] = [("possesses", "{{Sic|possesses|possess}}")],
		[6585] = [("possesses", "{{Sic|possesses|possess}}")],
		[6589] = [("possesses", "{{Sic|possesses|possess}}")],
		[6599] = [("possesses", "{{Sic|possesses|possess}}")],
		[6602] = [("possesses", "{{Sic|possesses|possess}}")],
		[6605] = [("possesses", "{{Sic|possesses|possess}}")],
		[6639] = [("possesses", "{{Sic|possesses|possess}}")],
		[6925] = [("Aldmer", "[[Lore:Aldmer|Aldmer]]")],
		[7082] = [("Daini", "[[Online:Daini|Daini]]")],
		[7180] = [("Karthald", "[[Online:Karthald|Karthald]]"), ("Jarl Olfwenn", "[[Online:Jarl Olfwenn|Jarl Olfwenn]]")],
		[7611] = [("journal", "[[ON:Disastrix Zansora's Journal|journal]]")],
		[7976] = [("waterlogged journal", "[[Online:Waterlogged Journal of Vanisande Maul|waterlogged journal]]")],
		[8077] = [("personal journal", "[[Online:Head Jailer's Journal|personal journal]]")],
		[8078] = [("report", "[[Online:Report for the Head Jailer|report]]")],
		[9081] = [("missive", "[[ON:Wormblood's Orders|missive]]")],
	};

	private static readonly HashSet<int> HackRemove =
	[
		4, 117, 208, 219, 259, 281, 282, 283, 284, 285, 289, 350, 428, 542, 606, 607, 636, 792, 796, 797, 845, 967, 1005, 1202, 1203, 1204, 1448, 1531, 1549, 1585, 1729, 1853, 1948, 1958, 1959, 1960, 1961, 2049, 2050, 2063, 2281, 2282, 2283, 2313, 2404, 2405, 2406, 2520, 2557, 2561, 2569, 2646, 2651, 2662, 2701, 2702, 2703, 2725, 2744, 2764, 2765, 2766, 2824, 2825, 2833, 2855, 2881, 2884, 2885, 2886, 2918, 2922, 2941, 2942, 2943, 3050, 3262, 3385, 3398, 3562, 3857, 4002, 4074, 4094, 4180, 4461, 4495, 4496, 4497, 4498, 4499, 4515, 4540, 4579, 4582, 4583, 4584, 4585, 4586, 4840, 4858, 4875, 4903, 4904, 4905, 4921, 4923, 4941, 5055, 5073, 5090, 5118, 5119, 5120, 5136, 5137, 5138, 5156, 5190, 5245, 5248, 5342, 5343, 5344, 5345, 5346, 5347, 5348, 5349, 5350, 5351, 5352, 5353, 5354, 5355, 5356, 5357, 5358, 5359, 5360, 5405, 5417, 5418, 5419, 5483, 5497, 5498, 5499, 5508, 5668, 5705, 5919, 5920, 5921, 6058, 6059, 6060, 6061, 6062, 6246, 6855, 6889, 6934, 6935, 7037, 7056, 7071, 7077, 7093, 7094, 7095, 7096, 7101, 7107, 7137, 7160, 7170, 7172, 7185, 7186, 7188, 7189, 7190, 7201, 7230, 7264, 7271, 7355, 7367, 7368, 7369, 7370, 7407, 7439, 7474, 7486, 7487, 7488, 7489, 7514, 7545, 7547, 7548, 7562, 7577, 7639, 7659, 7701, 7742, 7769, 7770, 7782, 7784, 7875, 7876, 7880, 7884, 7909, 7945, 7946, 7947, 7965, 7997, 7999, 8000, 8003, 8004, 8006, 8012, 8030, 8031, 8032, 8083, 8084, 8085, 8116, 8189, 8200, 8201, 8205, 8339, 8340, 8411, 8509, 8510, 8515, 8555, 8559, 8560, 8563, 8581, 8631, 8667, 8694, 8733, 8744, 8748, 8749, 8774, 8796, 8822, 8882, 8918, 8963, 8965, 9011, 9025, 9032, 9033, 9034, 9035, 9053, 9176, 9183, 9252, 9253, 9254, 9261, 9325, 9330, 9333, 9334, 9335, 9343, 9368, 9369, 9370, 9371, 9372, 9387, 9390, 9391, 9392, 9397, 9398, 9399, 9400, 9401, 9402, 9405, 9406, 9408, 9409, 9411, 9412, 9413, 9414, 9415, 9416, 9417, 9418, 9419, 9421, 9423, 9424, 9425, 9426, 9427, 9428, 9429, 9430, 9431, 9432, 9433, 9434, 9435, 9436, 9437, 9537, 9538, 9539, 9540, 9541, 9542, 9543, 9544, 9545, 9546, 9547, 9548, 9549, 9550, 9551, 9553, 9554, 9555, 9557, 9558, 9559, 9560, 9561, 9562, 9563, 9564, 9565, 9566, 9567, 9568, 9569, 9570, 9571, 9572, 9573, 9574, 9575, 9576, 9577, 9578, 9579, 9580, 9581, 9586, 9587, 9588, 9589, 9591, 9592, 9593, 9594, 9595, 9596, 9597, 9598, 9599, 9601, 9602, 9603, 9604, 9605, 9606, 9607, 9608, 9609, 9611, 9612, 9613, 9614, 9615, 9616, 9617, 9618, 9619, 9620, 9621, 9622, 9623, 9624, 9625, 9626, 9627, 9628, 9629, 9639
	];
	#endregion

	#region Fields
	private readonly FlatteningComparer comparer;
	private readonly Dictionary<int, QuestItem> items = [];
	private readonly SortedSet<int> unused = [];
	#endregion

	#region Constructors
	[JobInfo("Update Quest Items", "ESO")]
	public EsoUpdateQuestItems(JobManager jobManager)
		: base(jobManager)
	{
		var context = new Context(this.Site);
		var subComparer = this.Site.GetStringComparer(false);
		this.comparer = new FlatteningComparer(context, subComparer);
	}

	protected override string TemplateName => "Online Quest Item Entry";
	#endregion

	#region Private Properties
	private IReadOnlyDictionary<string, FilePage> Files => field ??= EsoFiles.GetOriginalFiles(this.Site);

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
			if (HackRemove.Contains(id))
			{
				continue;
			}

			var item = this.items[id];
			var abbr = string.Empty;
			var name = string.Empty;
			if (this.Files.TryGetValue(item.Icon, out var filePage))
			{
				var (_, abbr2, name2, _) = UespFunctions.AbbreviationFromIconName(this.NsList, filePage.Title.PageName);
				abbr = abbr2 ?? string.Empty;
				name = name2 ?? string.Empty;
			}

			this.WriteLine($"{{{{{this.TemplateName}|{item.Name}|icontype={abbr}|icon={name}|id={id}|loc=|quest={item.QuestName}|{item.Description}}}}}");
		}

		this.WriteLine("|}");
	}

	protected override void BeforeLoadPages()
	{
		var csvFile = new CsvFile(LocalConfig.BotDataSubPath("Quest Items.txt"))
		{
			EscapeCharacter = '\\',
			HasHeader = false
		};

		var questNameMap = new Dictionary<int, string>();
		foreach (var row in Database.RunQuery(EsoLog.Connection, "SELECT DISTINCT itemId, questName FROM questItem"))
		{
			var itemId = (int)row["itemId"];
			var questName = (string)row["questName"];
			questNameMap.Add(itemId, questName);
		}

		foreach (var row in csvFile.ReadRows())
		{
			var id = int.Parse(row[0], this.Site.Culture);
			var name = row[1];
			var description = row[2];
			var icon = row[3][1..^4];

			if (Replacements.GetValueOrDefault(id) is List<(string, string)> replacementList)
			{
				foreach (var (oldValue, newValue) in replacementList)
				{
					description = description.Replace(oldValue, newValue, StringComparison.Ordinal);
				}
			}

			questNameMap.TryGetValue(id, out var questName);
			this.items.Add(id, new QuestItem(name, description, icon, questName));
			this.unused.Add(id);
		}
	}

	protected override string GetEditSummary(Page page) => "Update quest item info";

	protected override void LoadPages() => this.Pages.GetBacklinks($"Template:{this.TemplateName}", BacklinksTypes.EmbeddedIn, true, Filter.Exclude);

	protected override void ParseTemplate(ITemplateNode template, SiteParser parser)
	{
		// Skip if id parameter is missing, invalid, or doesn't match an item in the dictionary.
		if (template.Find("id") is not IParameterNode idParam ||
			!int.TryParse(idParam.GetValue(), this.Site.Culture, out var id) ||
			!this.items.TryGetValue(id, out var item))
		{
			return;
		}

		this.unused.Remove(id);
		if (template.Find(1) is IParameterNode nameParam)
		{
			if (!this.comparer.Equals(nameParam.GetValue(), item.Name))
			{
				nameParam.SetValue(item.Name, ParameterFormat.Copy);
			}
		}
		else
		{
			template.Add(item.Name);
		}

		if (this.Files.TryGetValue(item.Icon, out var filePage))
		{
			var (_, abbr, name, _) = UespFunctions.AbbreviationFromIconName(this.NsList, filePage.Title.PageName);
			template.Update("icontype", abbr ?? string.Empty);
			template.Update("icon", name ?? string.Empty);
		}

		if (item.QuestName is not null)
		{
			var curValue = template.Find("quest")?.GetValue();
			if (curValue is null ||
				(!curValue.OrdinalEquals(item.QuestName) &&
				!(curValue.StartsWith(item.QuestName + " (", StringComparison.Ordinal) && curValue[^1] == ')')))
			{
				template.Update("quest", item.QuestName);
			}
		}

		if (template.Find(2) is IParameterNode descriptionParam)
		{
			if (!this.comparer.Equals(descriptionParam.GetValue(), item.Description))
			{
				descriptionParam.SetValue(item.Description, ParameterFormat.Copy);
			}
		}
		else
		{
			template.Add(item.Description);
		}
	}
	#endregion

	#region Private Classes
	private sealed record QuestItem(string Name, string Description, string Icon, string? QuestName);
	#endregion
}