namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;

	[method: JobInfo("NPCs from Lang")]
	internal sealed class GenderedNPCsFromLang(JobManager jobManager) : WikiJob(jobManager, JobType.ReadOnly)
	{
		#region Protected Override Methods
		protected override void Main()
		{
			TitleCollection esoNpcs = new(this.Site);
			esoNpcs.GetCategoryMembers("Online-NPCs");

			var fileName = LocalConfig.BotDataSubPath("en.lang.csv");
			var fileNameOut = LocalConfig.BotDataSubPath("GenderedNPCs.txt");
			CsvFile csvFile = new(fileName)
			{
				DoubleUpDelimiters = true
			};
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
		#endregion
	}
}