namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Diagnostics;
	using System.IO;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon;

	public class DBMergeFiles : PageMoverJob
	{
		[JobInfo("Merge Files", "Dragonborn Merge")]
		public DBMergeFiles(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
			this.MoveOptions = MoveOptions.MoveSubPages | MoveOptions.MoveTalkPage;
			this.FollowUpActions = FollowUpActions.EmitReport /*| FollowUpActions.ProposeUnused*/ | FollowUpActions.FixLinks | FollowUpActions.FixCaption;
			File.Delete(BacklinksFile);
			File.Delete(ReplacementStatusFile);
		}

		protected override void PopulateReplacements()
		{
			const string prefix = "File:SR-";

			var deleted = new TitleCollection(this.Site);
			deleted.GetCategoryMembers("Marked for Deletion");
			deleted.FilterToNamespaces(MediaWikiNamespaces.File);

			var dbFiles = new TitleCollection(this.Site);
			dbFiles.GetNamespace(MediaWikiNamespaces.File, Filter.Exclude, "DB-");
			foreach (var page in deleted)
			{
				dbFiles.Remove(page);
			}

			var srFiles = new TitleCollection(this.Site);
			srFiles.GetNamespace(MediaWikiNamespaces.File, Filter.Exclude, "SR-");

			foreach (var dbFile in dbFiles)
			{
				var fileName = dbFile.PageName.Substring(3).Replace(" 01.", ".", StringComparison.Ordinal);
				var extension = fileName.Substring(fileName.LastIndexOf('.'));
				fileName = fileName.Substring(0, fileName.Length - extension.Length);
				fileName = fileName switch
				{
					"creature-Ash Spawn2" => "creature-Ash Spawn 3",
					"creature-Ash Spawn 02" => "creature-Ash Spawn 4",
					"book-Fahlbtharzjournal02" => "book-Fahlbtharzjournal 02",
					"book-Fahlbtharzjournal03" => "book-Fahlbtharzjournal 03",
					_ => fileName
				};

				if (srFiles.Contains(prefix + fileName + extension))
				{
					int num;
					var found = true;
					var numLength = 1;
					for (num = 2; found; num++)
					{
						found = srFiles.Contains($"{prefix}{fileName} {num}{extension}");
						if (!found)
						{
							found = srFiles.Contains($"{prefix}{fileName} 0{num}{extension}");
							if (found && numLength == 1)
							{
								// Stupidly primitive, but it'll do for this.
								numLength = 2;
							}
						}
					}

					num--;
					var numText = "0" + num.ToString();
					numText = numText.Substring(numText.Length - numLength);
					fileName = $"{fileName} {numText}";
				}

				var srFile = new Title(this.Site, prefix + fileName + extension);
				if (dbFile.PageName.Replace("DB-", "SR-", StringComparison.Ordinal) != srFile.PageName)
				{
					Debug.WriteLine($"{dbFile.PageName} -> {srFile.PageName}");
				}

				var replacement = new Replacement(dbFile, srFile);
				this.Replacements.Add(replacement);
			}
		}
	}
}