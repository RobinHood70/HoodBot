namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	[method: JobInfo("One-Off Parse Job")]
	public class OneOffParseJob(JobManager jobManager) : ParsedPageJob(jobManager)
	{
		#region Fields
		private readonly HashSet<string> fauna = new(StringComparer.Ordinal);
		#endregion

		#region Public Override Properties
		public override string LogDetails => "Convert NPC Summary to Creature Summary";

		public override string LogName => "One-Off Parse Job";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => this.LogDetails;

		protected override void LoadPages()
		{
			this.GetFauna();
			this.Pages.GetBacklinks("Template:NPC Summary", BacklinksTypes.EmbeddedIn);
		}

		protected override void ParseText(ContextualParser parser)
		{
			var changed = false;
			var skipped = false;
			foreach (var template in parser.FindSiteTemplates("NPC Summary"))
			{
				var editorId = template.GetValue("eid");
				if (editorId is not null && this.fauna.Contains(editorId))
				{
					changed = true;
					template.Title.Clear();
					template.Title.AddText("Creature Summary\n");
					template.Remove("eid");
					template.RenameParameter("race", "species");
					template.Remove("gender");
					template.Remove("skills");
					var dead = template.GetValue("dead");
					if (dead is not null)
					{
						if (dead.Length > 0)
						{
							template.UpdateIfEmpty("health", "0", ParameterFormat.OnePerLine);
						}

						template.Remove("dead");
					}

					template.AddIfNotExists("refid", string.Empty, ParameterFormat.OnePerLine);
					template.AddIfNotExists("planet", string.Empty, ParameterFormat.OnePerLine);
					template.AddIfNotExists("biomes", string.Empty, ParameterFormat.OnePerLine);
					template.AddIfNotExists("species", string.Empty, ParameterFormat.OnePerLine);
					template.AddIfNotExists("predation", string.Empty, ParameterFormat.OnePerLine);
					template.Sort("refid", "baseid", "planet", "biomes", "species", "predation", "image", "imgdesc");
				}
				else
				{
					skipped = true;
				}
			}

			if (changed)
			{
				if (skipped)
				{
					Debug.WriteLine(parser.Title);
				}
				else if (parser.FindSiteTemplate("Stub") is SiteTemplateNode stub)
				{
					stub.Parameters.Clear();
					var faunaStub = parser.Factory.ParameterNodeFromParts("Fauna");
					stub.Parameters.Add(faunaStub);
				}
			}
		}
		#endregion

		#region Private Methods
		private void GetFauna()
		{
			var csv = new CsvFile(Starfield.ModFolder + "Fauna.csv");
			foreach (var row in csv.ReadRows())
			{
				this.fauna.Add(row["EditorID"]);
			}
		}
		#endregion
	}
}