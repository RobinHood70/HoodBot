namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using RobinHood70.CommonCode;

	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;

	public class BladesEffects : EditJob
	{
		#region Static Fields
		private static readonly string[] IgnoreList = new[]
		{
			"Black Green Smoke",
			"Continuous Frost Damage",
			"Continuous Poison Damage",
			"Continuous Untyped Damage",
			"Resist Hazards",
			"Templar Set Block Bonus",
			"Templar Set Burning Revenge",
			"Templar Set Continuous Fire Damage",
		};
		#endregion

		[JobInfo("Create Effects", "Blades|")]
		public BladesEffects(JobManager jobManager)
			: base(jobManager)
		{
		}

		protected override void BeforeLogging()
		{
			var fileName = UespSite.GetBotDataFolder("QuestLanguageDatabase.txt");
			var translation = new Dictionary<string, string>(StringComparer.Ordinal);
			var langFile = new CsvFile();
			langFile.ReadFile(fileName, true);
			foreach (var entry in langFile)
			{
				translation.Add(entry["ID"], entry["en-US"]);
			}

			fileName = UespSite.GetBotDataFolder("ItemPropertyList.txt");
			var lines = File.ReadAllLines(fileName);
			var codeLines = BladesCodeLine.Parse(lines);

			var entries = codeLines["_propertyList"]["size"];
			var allTitles = new TitleCollection(this.Site);
			foreach (var entry in entries)
			{
				if (entry["_editorName"].Value is string pageName)
				{
					allTitles.Add(UespNamespaces.Blades, pageName);
				}
				else
				{
					throw new InvalidOperationException("_editorName does not exist!");
				}
			}

			var exists = allTitles.Load(PageModules.Info);

			foreach (var entry in entries)
			{
				var desc = entry["_description"]["_key"].Value ?? throw new InvalidOperationException();
				desc = translation[desc]
					.Replace("{0} minute", "<duration> minute", StringComparison.Ordinal)
					.Replace("{0} second", "<duration> second", StringComparison.Ordinal)
					.Replace("{1} second", "<duration> second", StringComparison.Ordinal)
					.Replace("{0}", "<magnitude>", StringComparison.Ordinal)
					.Replace("{1}", "<magnitude2>", StringComparison.Ordinal)
					.Replace("{2}", "<magnitude3>", StringComparison.Ordinal);

				if (entry["_editorName"].Value is string pageName && !IgnoreList.Contains(pageName))
				{
					if (exists["Blades:" + pageName].Exists)
					{
						pageName += " (effect)";
					}

					var page = new Page(this.Site[UespNamespaces.Blades], pageName)
					{
						Text = string.Concat("{{Trail|Effects}}{{Minimal}}\n{{Effect Summary\ntype=\nimage=\nsyntax=", desc, "\n|notrail=1\n}}\n{{Stub|Effect}}")
					};

					this.Pages.Add(page);
				}
			}
		}

		protected override void Main() => this.SavePages("Create Effects Page");
	}
}
