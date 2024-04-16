namespace RobinHood70.HoodBot.Jobs.Loggers
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Properties;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WallE.Design;
	using RobinHood70.WikiCommon.Parser;

	public class PageJobLogger : JobLogger
	{
		#region Static Fields
		private static readonly InvalidOperationException BadLogPage = new(Resources.BadLogPage);
		#endregion

		#region Fields
		private readonly Title logTitle;
		private Page? logPage;
		private DateTime? end;
		private LogInfo? logInfo;
		private DateTime? start;
		private string status = "None";
		#endregion

		#region Constructors
		public PageJobLogger(Site site, string pageName)
		{
			this.logTitle = TitleFactory.FromUnvalidated(site.NotNull(), pageName.NotNull());
		}
		#endregion

		#region Public Override Methods
		public override void AddLogEntry(LogInfo info)
		{
			this.logInfo = info.NotNull();
			this.start = DateTime.UtcNow;
			this.end = null;
			this.status = info.Title;
			this.UpdateEntry();
			this.SaveLogPage("Job Started", this.UpdateEntry);
		}

		public override void EndLogEntry()
		{
			this.logInfo.PropertyThrowNull(nameof(PageJobLogger), nameof(this.logInfo));
			this.end = DateTime.UtcNow;
			this.status = "None";
			this.UpdateEntry();
		}

		public override void CloseLog()
		{
			if (this.logPage != null)
			{
				this.SaveLogPage("Job Finished", this.EndLogEntry);
			}

			this.start = null;
			this.end = null;
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
		private static bool UpdateCurrentStatus(ContextualParser parser, string status)
		{
			var currentTask = parser.FindIndex<IHeaderNode>(header => string.Equals(header.GetTitle(true), "Current Task", StringComparison.Ordinal));
			var taskLog = parser.FindIndex<IHeaderNode>(currentTask + 1);
			if (currentTask == -1 || taskLog == -1)
			{
				throw BadLogPage;
			}

			currentTask++;
			var section = parser.GetRange(currentTask, taskLog - currentTask);
			var previousTask = WikiTextVisitor.Raw(section).Trim().TrimEnd(TextArrays.Period);
			parser.RemoveRange(currentTask, taskLog - currentTask);
			parser.Insert(currentTask, parser.Factory.TextNode("\n" + status + ".\n\n"));

			return string.Equals(previousTask, status, StringComparison.Ordinal);
		}

		private void UpdateEntry()
		{
			Debug.Assert(this.logInfo != null, "LogInfo is null.");
			this.logPage ??= this.logTitle.Load();
			ContextualParser parser = new(this.logPage);
			var factory = parser.Factory;
			var sameTaskText = UpdateCurrentStatus(parser, this.status);
			var firstEntry = parser.FindIndex<SiteTemplateNode>(template => template.TitleValue.PageNameEquals("/Entry"));
			if (firstEntry == -1)
			{
				// CONSIDER: This used to insert a /Entry into an empty table, but given that we're not currently parsing tables, that would've required far too much code for a one-off situation, so it's been left out. Could theoretically be reintroduced once table parsing is in place.
				throw BadLogPage;
			}

			var entry = (SiteTemplateNode)parser[firstEntry];
			if (
				this.end == null &&
				sameTaskText &&
				string.IsNullOrEmpty(entry.GetValue(3)) &&
				this.logInfo.Title.OrdinalEquals(entry.GetValue(1)) &&
				this.logInfo.Details.OrdinalEquals(entry.Find("info")?.Value.ToValue() ?? string.Empty))
			{
				return;
			}

			if (this.end != null)
			{
				// If the last job was the same as this job and has no end time, then it's either the current job or a resumed one.
				var startParam = entry.FindNumberedIndex(2);
				if (startParam >= 0)
				{
					var endParam = factory.ParameterNodeFromParts(FormatDateTime(DateTime.UtcNow));
					entry.Parameters.Insert(startParam + 1, endParam);
					parser.UpdatePage();
					return;
				}
			}

			List<(string?, string)> parms = [(null, this.logInfo.Title)];
			if (!string.IsNullOrEmpty(this.logInfo.Details))
			{
				parms.Add(("info", this.logInfo.Details));
			}

			AddDateTime(parms, this.start);
			AddDateTime(parms, this.end);

			List<IWikiNode> nodes =
				[
					factory.TemplateNodeFromParts("/Entry", false, parms),
					factory.TextNode("\n")
				];
			parser.InsertRange(firstEntry, nodes);
			parser.UpdatePage();
		}

		private void SaveLogPage(string editSummary, Action editConflictAction)
		{
			var saved = false;
			do
			{
				// Assumes that its current LogPage.Text is still valid and tries to update, then save that directly. Loads only if it gets an edit conflict.
				try
				{
					var result = this.logPage?.Save(editSummary, true);
					saved = result is
						ChangeStatus.EditingDisabled or
						ChangeStatus.NoEffect or
						ChangeStatus.Success;
				}
				catch (EditConflictException)
				{
					this.logPage = null;
					editConflictAction();
				}
				catch (StopException)
				{
					break;
				}
			}
			while (!saved);
		}
		#endregion
	}
}