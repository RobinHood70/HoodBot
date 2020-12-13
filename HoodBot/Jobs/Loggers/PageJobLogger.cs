namespace RobinHood70.HoodBot.Jobs.Loggers
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs;
	using RobinHood70.HoodBot.Properties;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WallE.Design;
	using RobinHood70.WikiCommon.Parser;
	using static RobinHood70.CommonCode.Globals;

	public class PageJobLogger : JobLogger
	{
		#region Static Fields
		private static readonly InvalidOperationException BadLogPage = new InvalidOperationException(Resources.BadLogPage);
		#endregion

		#region Fields
		private readonly Page logPage;
		private DateTime? end;
		private LogInfo? logInfo;
		private DateTime? start;
		private string? status;
		#endregion

		#region Constructors
		public PageJobLogger(Site site, string pageName, JobTypes typesToLog)
			: base(typesToLog)
		{
			ThrowNull(site, nameof(site));
			ThrowNull(pageName, nameof(pageName));
			this.logPage = Page.FromName(site, pageName);
		}
		#endregion

		#region Public Override Methods
		public override void AddLogEntry(LogInfo info)
		{
			ThrowNull(info, nameof(info));
			this.logInfo = info;
			this.start = DateTime.UtcNow;
			this.UpdateLogPage("Job Started", info.Title);
		}

		public override void EndLogEntry()
		{
			ThrowNull(this.logInfo, nameof(PageJobLogger), nameof(this.logInfo));
			this.end = DateTime.UtcNow;
			this.UpdateLogPage("Job Finished", "None");
		}
		#endregion

		#region Private Static Methods
		private static void AddDateTime(List<(string? Name, string Value)> parms, DateTime? dateTime)
		{
			if (dateTime.HasValue)
			{
				parms.Add((null, FormatDateTime(dateTime.Value)));
			}
		}

		private static string FormatDateTime(DateTime dt) => dt.ToString("u", CultureInfo.InvariantCulture).TrimEnd('Z');
		#endregion

		#region Private Methods
		private bool UpdateCurrentStatus(ContextualParser parser)
		{
			ThrowNull(this.status, nameof(PageJobLogger), nameof(this.status));
			var currentTask = parser.Nodes.FindIndex<IHeaderNode>(header => string.Equals(header.GetInnerText(true), "Current Task", StringComparison.Ordinal));
			var taskLog = parser.Nodes.FindIndex<IHeaderNode>(currentTask + 1);
			if (currentTask == -1 || taskLog == -1)
			{
				throw BadLogPage;
			}

			currentTask++;
			var section = parser.Nodes.GetRange(currentTask, taskLog - currentTask);
			var previousTask = WikiTextVisitor.Raw(section).Trim().TrimEnd(TextArrays.Period);
			parser.Nodes.RemoveRange(currentTask, taskLog - currentTask);
			parser.Nodes.Insert(currentTask, parser.Nodes.Factory.TextNode("\n" + this.status + ".\n\n"));

			return string.Equals(previousTask, this.status, StringComparison.Ordinal);
		}

		private void UpdateEntry(Page sender, EventArgs eventArgs)
		{
			Debug.Assert(this.logInfo != null, "LogInfo is null.");
			var parser = new ContextualParser(sender);
			var factory = parser.Nodes.Factory;
			var result = this.UpdateCurrentStatus(parser);
			var firstEntry = parser.Nodes.FindIndex<SiteTemplateNode>(template => template.TitleValue.PageNameEquals("/Entry"));
			if (firstEntry != -1)
			{
				var entry = (SiteTemplateNode)parser.Nodes[firstEntry];

				// If the last job was the same as this job and has no end time, then it's either the current job or a resumed one.
				if (string.IsNullOrEmpty(entry.Find(3)?.Value.ToValue()) &&
					string.Equals(entry.Find(1)?.Value.ToValue().Trim(), this.logInfo.Title, StringComparison.Ordinal) &&
					string.Equals(entry.Find("info")?.Value.ToValue() ?? string.Empty, this.logInfo.Details ?? string.Empty, StringComparison.Ordinal))
				{
					// If the end date is not null, then we're at the end of the job, so update the end time.
					if (this.end != null)
					{
						var startParam = entry.FindNumberedIndex(2);
						Debug.Assert(startParam != -1, "Start parameter not found.");
						var endParam = factory.ParameterNodeFromParts(FormatDateTime(DateTime.UtcNow));
						entry.Parameters.Insert(startParam, endParam);
						sender.Text = parser.ToRaw();
					}

					return;
				}

				var parms = new List<(string?, string)> { (null, this.logInfo.Title) };
				if (!string.IsNullOrEmpty(this.logInfo.Details))
				{
					parms.Add(("info", this.logInfo.Details));
				}

				AddDateTime(parms, this.start);
				AddDateTime(parms, this.end);

				parser.Nodes.InsertRange(firstEntry, new IWikiNode[]
				{
					factory.TemplateNodeFromParts("/Entry", false, parms),
					factory.TextNode("\n")
				});

				sender.Text = parser.ToRaw();
			}
			else
			{
				// CONSIDER: This used to insert a /Entry into an empty table, but given that we're not currently parsing tables, that would've required far too much code for a one-off situation, so it's been left out. Could theoretically be reintroduced once table parsing is in place.
				throw BadLogPage;
			}
		}

		private void UpdateLogPage(string editSummary, string status)
		{
			this.status = status;
			var saved = false;
			this.logPage.PageLoaded += this.UpdateEntry;
			if (this.logPage.IsLoaded)
			{
				this.UpdateEntry(this.logPage, EventArgs.Empty);
			}
			else
			{
				this.logPage.Load();
			}

			do
			{
				// Assumes that its current LogPage.Text is still valid and tries to update, then save that directly. Loads only if it gets an edit conflict.
				try
				{
					var result = this.logPage.Save(editSummary, true);
					saved = result == ChangeStatus.EditingDisabled || result == ChangeStatus.NoEffect || result == ChangeStatus.Success;
				}
				catch (EditConflictException)
				{
					this.logPage.Load();
				}
				catch (StopException)
				{
					saved = true; // Lie about it so the job stops.
				}
			}
			while (!saved);
			this.logPage.PageLoaded -= this.UpdateEntry;
		}
		#endregion
	}
}