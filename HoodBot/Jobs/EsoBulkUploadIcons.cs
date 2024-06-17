namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon;

	internal sealed class EsoBulkUploadIcons : WikiJob
	{
		#region Private Constants
		private const string RemoteIconPath = "/esoui/art/icons/";
		private const string Query = "SELECT id, name, icon FROM collectibles WHERE categoryName IN('Armor Styles', 'Weapon Styles') AND icon LIKE '" + RemoteIconPath + "%'";
		#endregion

		#region Static Fields
		private static readonly Dictionary<long, string> NameFixes = new()
		{
			[6117] = "Honor Guard Jack",
		};

		private static readonly List<Part> Parts =
		[
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
		];

		private readonly List<string> styles;
		#endregion

		#region Fields
		private List<Upload>? uploads;
		#endregion

		#region Constructors
		[JobInfo("Bulk Upload Style Icons", "ESO Update")]
		public EsoBulkUploadIcons(JobManager jobManager, string styles)
			: base(jobManager, JobType.Write)
		{
			var styleSplit = styles.Split(TextArrays.NewLineChars, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			this.styles = new List<string>(styleSplit.Length);
			foreach (var style in styleSplit)
			{
				this.styles.Add(style.Replace(" Style", string.Empty, StringComparison.OrdinalIgnoreCase));
			}

			this.styles.Sort(StringComparer.Ordinal);
		}
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			var patchVersion = this.GetPatchVersion("botitemset");
			this.GetIcons(patchVersion.Text, false);
			var allFiles = Directory.GetFiles(LocalConfig.WikiIconsFolder);
			HashSet<string> files = new(allFiles.Length, StringComparer.OrdinalIgnoreCase);

			TitleCollection fileTitles = new(this.Site);
			fileTitles.GetNamespace(MediaWikiNamespaces.File, Filter.Any, "ON-icon-");
			foreach (var file in allFiles)
			{
				var fileName = Path.GetFileName(file);
				files.Add(fileName);
			}

			this.uploads = this.GetUploads(files, fileTitles);
		}

		protected override void Main()
		{
			if (this.uploads?.Count > 0)
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
					var fileName = Path.Combine(LocalConfig.WikiIconsFolder, upload.Icon + ".png");
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
					name = EsoLog.ConvertEncoding((string)row["name"]);
				}

				var icon = EsoLog.ConvertEncoding((string)row["icon"]);
				iconLookup.Add(name, (id, icon[RemoteIconPath.Length..].Replace(".dds", ".png", StringComparison.Ordinal)));
			}

			return iconLookup;
		}
		#endregion

		#region Private Methods
		private List<Upload> GetUploads(HashSet<string> files, TitleCollection fileTitles)
		{
			List<Upload> retval = new(this.styles.Count * Parts.Count);
			var iconLookup = GetIcons();
			foreach (var style in this.styles)
			{
				this.WriteLine($"==[[Online:{style} Style|]]==");
				this.WriteLine("<gallery mode=nolines widths=64>");
				foreach (var part in Parts)
				{
					var dbName = $"{style} {part.Name}";
					if (iconLookup.TryGetValue(dbName, out var idIcon) && files.Contains(idIcon.Icon))
					{
						Upload upload = new(idIcon.Id, idIcon.Icon, style, part);
						var exists = fileTitles.Contains("File:" + upload.DestinationName);
						this.Write($"{upload.DestinationName}|{part.Name}");
						if (exists)
						{
							this.WriteLine(" (already exists)");
						}
						else
						{
							this.WriteLine();
							retval.Add(upload);
						}
					}
				}

				this.WriteLine();
			}

			retval.TrimExcess();
			return retval;
		}
		#endregion

		#region Private Classes
		private sealed class Part(string name, string type, string bodyPart)
		{
			#region Public Properties
			public string BodyPart { get; } = bodyPart;

			public string Name { get; } = name;

			public string Type { get; } = type;
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
				var partType = string.Equals(part.Type, "weapons", StringComparison.Ordinal) ? "weapon" : part.Type;
				this.DestinationName = $"ON-icon-{partType}-{part.Name}-{style}.png";
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