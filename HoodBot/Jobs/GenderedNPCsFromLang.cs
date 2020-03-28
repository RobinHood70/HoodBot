namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;

	public class GenderedNPCsFromLang : WikiJob
	{
		[JobInfo("NPCs from Lang")]
		public GenderedNPCsFromLang([ValidatedNotNull] Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
		}

		protected override void Main()
		{
			var esoNpcs = new TitleCollection(this.Site);
			esoNpcs.GetCategoryMembers("Online-NPCs");

			var fileName = Environment.ExpandEnvironmentVariables(@"%BotData%\en.lang.csv");
			var fileNameOut = Environment.ExpandEnvironmentVariables(@"%BotData%\GenderedNPCs.txt");
			using var reader = File.OpenText(fileName);
			var csvFile = new CsvFile
			{
				DoubleUpDelimiters = true
			};
			csvFile.ReadText(reader, true);
			var npcs = new SortedSet<string>();
			foreach (var row in csvFile)
			{
				if (int.Parse(row["ID"]) == 8290981)
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