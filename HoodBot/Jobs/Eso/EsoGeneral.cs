namespace RobinHood70.HoodBot.Jobs.Eso
{
	using System.Collections.Generic;
	using System.Configuration;
	using System.Data;
	using System.Text.RegularExpressions;
	using MySql.Data.MySqlClient;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiClasses;
	using static RobinHood70.WikiCommon.Globals;

	#region Public Enumerations
	public enum Gender
	{
		Unknown = 0,
		Female = 1,
		Male = 2
	}
	#endregion

	internal static class EsoGeneral
	{
		#region Private Constants
		private const string PatchPageName = "Online:Patch";
		#endregion

		#region Static Fields
		private static readonly Regex BonusFinder = new Regex(@"\s*Current [Bb]onus:.*?\.");
		private static readonly Regex SpaceFixer = new Regex(@"[\n\ ]+");
		private static readonly string EsoLogConnectionString = ConfigurationManager.ConnectionStrings["EsoLog"].ConnectionString;
		private static string patchVersion = null;
		#endregion

		#region Public Properties
		public static Dictionary<int, string> MechanicNames { get; } = new Dictionary<int, string>
		{
			[-2] = "Health",
			[0] = "Magicka",
			[6] = "Stamina",
			[10] = "Ultimate",
			[-50] = "Ultimate (no weapon damage)",
			[-51] = "Light Armor #",
			[-52] = "Medium Armor #",
			[-53] = "Heavy Armor #",
			[-54] = "Dagger #",
			[-55] = "Armor Type #",
			[-56] = "Spell + Weapon Damage",
			[-57] = "Assassination Skills Slotted",
			[-58] = "Fighters Guild Skills Slotted",
			[-59] = "Draconic Power Skills Slotted",
			[-60] = "Shadow Skills Slotted",
			[-61] = "Siphoning Skills Slotted",
			[-62] = "Sorcerer Skills Slotted",
			[-63] = "Mages Guild Skills Slotted",
			[-64] = "Support Skills Slotted",
			[-65] = "Animal Companion Skills Slotted",
			[-66] = "Green Balance Skills Slotted",
			[-67] = "Winter's Embrace Slotted",
			[-68] = "Magicka with Health Cap",
			[-69] = "Magicka with Health Cap",
		};
		#endregion

		#region Public Methods
		public static Dictionary<long, NPCData> GetNpcsFromDatabase()
		{
			var tempNpcData = new Dictionary<long, NPCData>();
			foreach (var row in RunQuery("SELECT id, name, gender, ppClass FROM uesp_esolog.npc WHERE level != -1"))
			{
				var name = ((string)row["name"]).TrimEnd(); // TrimEnd() corrects a single record where the field has a tab at the end of it - seems to be an ESO problem
				var id = (long)row["id"];
				var npcData = new NPCData(name, (sbyte)row["gender"], (string)row["ppClass"]);
				tempNpcData.Add(id, npcData);
			}

			return tempNpcData;
		}

		public static string GetPatchVersion(WikiJob job)
		{
			if (patchVersion == null)
			{
				job.StatusWriteLine("Fetching ESO update number");
				var patchTitle = new TitleCollection(job.Site, PatchPageName);
				var patchPage = patchTitle.Load(new PageLoadOptions(PageModules.Custom), (job.Site.PageCreator as MetaTemplateCreator) ?? new MetaTemplateCreator())[0] as VariablesPage;
				patchVersion = patchPage.VariableSets[string.Empty]["number"];
			}

			return patchVersion;
		}

		public static string HarmonizeDescription(string desc) => SpaceFixer.Replace(BonusFinder.Replace(desc, string.Empty), " ");

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "No user input.")]
		public static IEnumerable<IDataRecord> RunQuery(string query)
		{
			using (var connection = new MySqlConnection(EsoLogConnectionString))
			{
				connection.Open();
				using (var command = new MySqlCommand(query, connection))
				using (var reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						yield return reader;
					}
				}
			}
		}

		public static void SetBotUpdateVersion(WikiJob job, string pageType)
		{
			// Assumes EsoPatchVersion has already been updated.
			ThrowNull(pageType, nameof(pageType));
			job.StatusWriteLine("Update patch bot parameters");
			var paramName = "bot" + pageType;
			var patchPage = new TitleCollection(job.Site, PatchPageName).Load()[0];
			var match = Template.Find("Online Patch").Match(patchPage.Text);
			var patchTemplate = Template.Parse(match.Value);
			var oldValue = patchTemplate[paramName]?.Value;
			var patchVersion = GetPatchVersion(job);
			if (oldValue != patchVersion)
			{
				patchTemplate.AddOrChange(paramName, patchVersion);
				patchPage.Text = patchPage.Text
					.Remove(match.Index, match.Length)
					.Insert(match.Index, patchTemplate.ToString());

				patchPage.Save("Update " + paramName, true);
			}
		}

		public static string TimeToText(int time) => ((double)time).ToString("0,.#");
		#endregion
	}
}