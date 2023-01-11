namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using RobinHood70.CommonCode;

	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;

	internal sealed class GenderedNPCsFromLang : WikiJob
	{
		[JobInfo("NPCs from Lang")]
		public GenderedNPCsFromLang(JobManager jobManager)
			: base(jobManager, JobType.ReadOnly)
		{
		}

		protected override void Main()
		{
			TitleCollection esoNpcs = new(this.Site);
			esoNpcs.GetCategoryMembers("Online-NPCs");

			var fileName = UespSite.GetBotDataFolder("en.lang.csv");
			var fileNameOut = UespSite.GetBotDataFolder("GenderedNPCs.txt");
			using var reader = File.OpenText(fileName);
			CsvFile csvFile = new()
			{
				DoubleUpDelimiters = true
			};
			csvFile.ReadText(reader, true);
			SortedSet<string> npcs = new(System.StringComparer.Ordinal);
			foreach (var row in csvFile)
			{
				if (int.Parse(row["ID"], CultureInfo.InvariantCulture) == 8290981)
				{
					var name = row["Text"];
					if (name[^2] == '^')
					{
						name = name[0..^2];
						if (!esoNpcs.Contains("Online:" + name))
						{
							npcs.Add(name);
						}
					}
				}
			}

			File.WriteAllLines(fileNameOut, npcs);
		}
	}
}