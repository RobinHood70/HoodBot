namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon;

	// Images should be downloaded from latest version on https://esofiles.uesp.net/ in the icons.zip file before running this job.
	internal sealed class BulkUploadIcons : EditJob
	{
		#region Static Fields
		private static readonly Dictionary<long, string> NameFixes = new()
		{
			[6117] = "Honor Guard Jack",
		};

		private static readonly List<Part> Parts = new()
		{
			new("Hat", "armor", "Head"),
			new("Epaulets", "armor", "Shoulder"),
			new("Jerkin", "armor", "Chest"),
			new("Robe", "armor", "Chest"),
			new("Gloves", "armor", "Hands"),
			new("Sash", "armor", "Waist"),
			new("Breeches", "armor", "Legs"),
			new("Shoes", "armor", "Feet"),
			new("Helmet", "armor", "Head"),
			new("Arm Cops", "armor", "Shoulder"),
			new("Jack", "armor", "Chest"),
			new("Bracers", "armor", "Hands"),
			new("Belt", "armor", "Waist"),
			new("Guards", "armor", "Legs"),
			new("Boots", "armor", "Feet"),
			new("Helm", "armor", "Head"),
			new("Pauldrons", "armor", "Shoulder"),
			new("Cuirass", "armor", "Chest"),
			new("Gauntlets", "armor", "Hands"),
			new("Girdle", "armor", "Waist"),
			new("Greaves", "armor", "Legs"),
			new("Sabatons", "armor", "Feet"),
			new("Shield", "armor", "Shield"),
			new("Dagger", "weapons", "Daggers"),
			new("Sword", "weapons", "Swords"),
			new("Axe", "weapons", "Axes"),
			new("Mace", "weapons", "Maces"),
			new("Greatsword", "weapons", "Greatswords"),
			new("Battle Axe", "weapons", "Battle Axes"),
			new("Maul", "weapons", "Mauls"),
			new("Bow", "weapons", "Bows"),
			new("Staff", "weapons", "Staves"),
		};

		private static readonly List<string> Styles = new()
		{
			"Ascendant Order",
			"Dragonguard Berserker",
			"Dreadsails",
			"Reefborn",
			"Saberkeel Panoply",
		};

		private static readonly string Query = "SELECT id, name, icon FROM collectibles WHERE categoryName IN('Armor Styles', 'Weapon Styles');";

		private static readonly string WikiIconFolder = Path.Combine(UespSite.GetBotDataFolder(), "WikiIcons");
		#endregion

		#region Fields
		private readonly List<Upload> uploads = new();
		#endregion

		#region Constructors
		[JobInfo("Bulk Upload Icons", "ESO")]
		public BulkUploadIcons(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			var iconLookup = GetIcons();
			var allFiles = Directory.GetFiles(WikiIconFolder);
			HashSet<string> files = new(allFiles.Length, StringComparer.OrdinalIgnoreCase);

			TitleCollection fileTitles = new(this.Site);
			fileTitles.GetNamespace(MediaWikiNamespaces.File, Filter.Any, "ON-icon-");
			foreach (var file in allFiles)
			{
				var fileName = Path.GetFileName(file);
				files.Add(fileName);
			}

			Styles.Sort(StringComparer.Ordinal);
			foreach (var style in Styles)
			{
				this.WriteLine($"=={style} Style==");
				foreach (var part in Parts)
				{
					var dbName = $"{style} {part.Name}";
					if (!iconLookup.TryGetValue(dbName, out var idIcon))
					{
						continue;
					}

					if (files.Contains(idIcon.Icon))
					{
						Upload upload = new(idIcon.Id, idIcon.Icon, style, part);
						if (!fileTitles.Contains("File:" + upload.DestinationName))
						{
							this.WriteLine($"* [[:File:{upload.DestinationName}|{part.BodyPart}]]");
							this.uploads.Add(upload);
						}
					}
				}

				this.WriteLine();
			}

			this.uploads.TrimExcess();
		}

		protected override void Main()
		{
			if (this.uploads.Count > 0)
			{
				this.ProgressMaximum = this.uploads.Count;
				foreach (var upload in this.uploads)
				{
					var typeUcfirst = upload.Part.Type.UpperFirst(this.Site.Culture);
					var pageText = $"== Summary ==\n" +
						$"Original file: {upload.Icon}<br>\n" +
						$"Used for:\n" +
						$":Collectible: {{{{Item Link|{upload.Style} {upload.Part.Name}|collectid={upload.Id}}}}}\n" +
						$"\n" +
						$"[[Category:Online-Icons-{typeUcfirst}-{upload.Style}]]\n" +
						$"[[Category:Online-Icons-{typeUcfirst}-{upload.Part.BodyPart}]]\n" +
						$"== Licensing ==\n" +
						$"{{{{Zenimage}}}}";
					var fileName = Path.Combine(WikiIconFolder, upload.Icon + ".png");
					this.Site.Upload(fileName, upload.DestinationName, "Bulk upload ESO style icons", pageText);
				}
			}
		}
		#endregion

		#region Private Static Methods
		private static Dictionary<string, (long Id, string Icon)> GetIcons()
		{
			Dictionary<string, (long, string)> iconLookup = new(StringComparer.Ordinal);
			foreach (var row in Database.RunQuery(EsoLog.Connection, Query))
			{
				var id = (long)row["id"];
				if (!NameFixes.TryGetValue(id, out var name))
				{
					name = (string)row["name"];
				}

				var icon = (string)row["icon"];
				if (icon.StartsWith("/esoui/art/icons/", StringComparison.Ordinal))
				{
					iconLookup.Add(name, (id, icon[17..].Replace(".dds", ".png", StringComparison.Ordinal)));
				}
			}

			return iconLookup;
		}
		#endregion

		#region Private Classes
		private sealed class Part
		{
			#region Constructors
			public Part(string name, string type, string bodyPart)
			{
				this.Name = name;
				this.Type = type;
				this.BodyPart = bodyPart;
			}
			#endregion

			#region Public Properties
			public string BodyPart { get; }

			public string Name { get; }

			public string Type { get; }
			#endregion

		}

		private sealed class Upload
		{
			#region Constructors
			public Upload(long id, string icon, string style, Part part)
			{
				this.Icon = icon.Replace(".png", string.Empty, StringComparison.Ordinal);
				this.Id = id;
				this.Style = style;
				this.Part = part;
				var name = string.Equals(part.Name, "Battle Axe", StringComparison.Ordinal) ? "Battleaxe" : part.Name;
				var partType = string.Equals(part.Type, "weapons", StringComparison.Ordinal) ? "weapon" : part.Type;
				this.DestinationName = $"ON-icon-{partType}-{name}-{style}.png";
			}

			public string DestinationName { get; }

			public string Icon { get; }

			public long Id { get; }

			public Part Part { get; }

			public string Style { get; }
			#endregion
		}
		#endregion
	}
}
