namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;
	using RobinHood70.WikiCommon.Parser.Basic;

	internal sealed class VariableUsage : WikiJob
	{
		#region Fields
		private readonly List<LoadSaveCall> loadCalls = new();
		private readonly List<LoadSaveCall> saveCalls = new();
		#endregion

		#region Constructors
		[JobInfo("Variable Usage")]
		public VariableUsage(JobManager jobManager)
			: base(jobManager, JobType.ReadOnly)
		{
		}
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
				var nodes = new WikiNodeFactory().Parse(page.Text);
				foreach (var template in nodes.FindAll<ITemplateNode>())
				{
					var name = template.GetTitleText();
					if (name.StartsWith("#load", StringComparison.OrdinalIgnoreCase))
					{
						this.loadCalls.Add(new LoadSaveCall(page, template));
					}
					else if (name.StartsWith("#save", StringComparison.OrdinalIgnoreCase))
					{
						this.saveCalls.Add(new LoadSaveCall(page, template));
					}
				}
			}

			var comparer = SimpleTitleComparer.Instance;
			this.loadCalls.Sort((x, y) => comparer.Compare(x.Page, y.Page));
			this.saveCalls.Sort((x, y) => comparer.Compare(x.Page, y.Page));
		}

		private void WriteFile(bool isLoadCall)
		{
			CsvFile csvFile = new();
			//// csvFile.EmptyFieldText = " ";

			var list = isLoadCall ? this.loadCalls : this.saveCalls;
			var maxCount = 0;
			foreach (var call in list)
			{
				if (call.Variables.Count > maxCount)
				{
					maxCount = call.Variables.Count;
				}
			}

			List<string> header = new(maxCount + 3)
			{
				"Template Name"
			};

			if (isLoadCall)
			{
				header.Add("Load Page");
			}

			header.Add("Subset");

			for (var i = 1; i <= maxCount; i++)
			{
				header.Add("Variable " + i.ToStringInvariant());
			}

			csvFile.Header = header;

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

				cells.Add(call.Subset);
				cells.AddRange(call.Variables);
				csvFile.Add(cells);
			}

			var type = isLoadCall ? "Loaded" : "Saved";
			Debug.WriteLine($"{type}: {csvFile.Count} rows");
			csvFile.WriteFile(LocalConfig.BotDataSubPath(type + " Variables.txt"));
		}
		#endregion

		#region Private Classes
		private sealed class LoadSaveCall
		{
			public LoadSaveCall(Title page, ITemplateNode loadSave)
			{
				List<string> variables = new();
				this.Page = page;
				var title = loadSave.Title.ToRaw();
				var split = title.Split(TextArrays.Colon, 2);
				var name = split[0];
				var first = split[1];
				if (string.Equals(name, "#load", StringComparison.OrdinalIgnoreCase))
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
						case "subset":
							this.Subset = param.Value.ToRaw();
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

			public string Subset { get; } = string.Empty;

			public IReadOnlyList<string> Variables { get; }
		}
		#endregion
	}
}