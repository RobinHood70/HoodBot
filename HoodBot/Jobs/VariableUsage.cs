namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	[method: JobInfo("Variable Usage")]
	internal sealed class VariableUsage(JobManager jobManager) : WikiJob(jobManager, JobType.ReadOnly)
	{
		#region Fields
		private readonly List<LoadSaveCall> loadCalls = [];
		private readonly List<LoadSaveCall> saveCalls = [];
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			this.StatusWriteLine("Loading pages");
			this.ProgressMaximum = 2;
			var results = PageCollection.Unlimited(this.Site);
			results.GetNamespace(MediaWikiNamespaces.Template);
			this.Progress++;
			this.StatusWriteLine("Exporting");
			this.GetCalls(results);
			this.ExportResults();
			this.Progress++;
		}
		#endregion

		#region Private Static Methods
		private static List<string> BuildHeader(bool isLoadCall, int maxCount)
		{
			List<string> header = new(maxCount + 3)
			{
				"Template Name"
			};

			if (isLoadCall)
			{
				header.Add("Load Page");
			}

			header.Add("Set");

			for (var i = 1; i <= maxCount; i++)
			{
				header.Add("Variable " + i.ToStringInvariant());
			}

			return header;
		}
		#endregion

		#region Private Methods
		private void ExportResults()
		{
			try
			{
				this.WriteFile(true);
				this.WriteFile(false);
				this.StatusWriteLine("Files saved to bot folder.");
			}
			catch (System.IO.IOException e)
			{
				this.StatusWriteLine("Couldn't save files to bot folder.");
				this.StatusWriteLine(e.Message);
			}
		}

		private void GetCalls(PageCollection pages)
		{
			// TODO: Add a dictionary that can be pre-populated to translate synonyms to a consistent name. Similarly, name comparison can be case-sensitive or not. Need to find a useful way to do those.
			foreach (var page in pages)
			{
				var factory = new SiteNodeFactory(this.Site);
				var nodes = factory.Parse(page.Text);
				var collection = new WikiNodeCollection(factory, nodes);
				foreach (var template in collection.FindAll<ITemplateNode>())
				{
					var name = template.GetTitleText();
					if (name.StartsWith("#load", StringComparison.OrdinalIgnoreCase))
					{
						this.loadCalls.Add(new LoadSaveCall(page.Title, template));
					}
					else if (name.StartsWith("#save", StringComparison.OrdinalIgnoreCase))
					{
						this.saveCalls.Add(new LoadSaveCall(page.Title, template));
					}
				}
			}

			this.loadCalls.Sort((x, y) => TitleComparer.Instance.Compare(x.Page, y.Page));
			this.saveCalls.Sort((x, y) => TitleComparer.Instance.Compare(x.Page, y.Page));
		}

		private void WriteFile(bool isLoadCall)
		{
			var list = isLoadCall ? this.loadCalls : this.saveCalls;
			var maxCount = 0;
			foreach (var call in list)
			{
				if (call.Variables.Count > maxCount)
				{
					maxCount = call.Variables.Count;
				}
			}

			var type = isLoadCall ? "Loaded" : "Saved";
			var csvFile = new CsvFile(LocalConfig.BotDataSubPath(type + " Variables.txt"))
			{
				Header = BuildHeader(isLoadCall, maxCount)
			};

			foreach (var call in list)
			{
				List<string> cells = new(call.Variables.Count + 3)
				{
					call.Page.PageName
				};

				if (isLoadCall)
				{
					cells.Add(call.LoadPage);
				}

				cells.Add(call.Set);
				cells.AddRange(call.Variables);
				csvFile.Add(cells);
			}

			this.StatusWriteLine($"{type}: {csvFile.Count} rows");
			csvFile.Save();
		}
		#endregion

		#region Private Classes
		private sealed class LoadSaveCall
		{
			public LoadSaveCall(Title page, ITemplateNode loadSave)
			{
				List<string> variables = [];
				this.Page = page;
				var title = loadSave.TitleNodes.ToRaw();
				var split = title.Split(TextArrays.Colon, 2);
				var name = split[0];
				var first = split[1];
				if (name.OrdinalICEquals("#load"))
				{
					this.LoadPage = first;
				}
				else
				{
					variables.Add(first);
				}

				foreach (var param in loadSave.Parameters)
				{
					switch (param.Name?.ToValue())
					{
						case null:
							variables.Add(param.Value.ToRaw());
							break;
						case "set":
							this.Set = param.Value.ToRaw();
							break;
						case "if":
						case "ifnot":
							break;
						default:
							var fullParam = param.ToKeyValue();
							if (fullParam.Length > 0)
							{
								variables.Add(fullParam);
							}

							break;
					}
				}

				this.Variables = variables;
			}

			public string LoadPage { get; } = string.Empty;

			public Title Page { get; }

			public string Set { get; } = string.Empty;

			public List<string> Variables { get; }
		}
		#endregion
	}
}