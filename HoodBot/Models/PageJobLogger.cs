﻿namespace RobinHood70.HoodBot.Models
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using RobinHood70.HoodBot.Properties;
	using RobinHood70.Robby;
	using RobinHood70.WallE.Design;
	using RobinHood70.WikiClasses.Parser;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

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
		public PageJobLogger(JobTypes typesToLog, Page logPage)
			: base(typesToLog) => this.logPage = logPage ?? throw ArgumentNull(nameof(logPage));
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

		private static string FormatDateTime(DateTime dt) => dt.ToString("u").TrimEnd('Z');
		#endregion

		#region Private Methods
		private void AddTemplateToPage(LinkedListNode<IWikiNode> node)
		{
			var parms = new List<(string?, string)>
				{
					(null, this.logInfo.Title)
				};

			if (!string.IsNullOrEmpty(this.logInfo.Details))
			{
				parms.Add(("info", this.logInfo.Details));
			}

			AddDateTime(parms, this.start);
			AddDateTime(parms, this.end);

			var list = node.List!;
			list.AddBefore(node, TemplateNode.FromParts("/Entry", false, parms));
			list.AddBefore(node, new TextNode("\n"));
		}

		private bool UpdateCurrentStatus(NodeCollection nodes)
		{
			ThrowNull(this.status, nameof(PageJobLogger), nameof(this.status));
			var currentTask = nodes.FindFirstHeaderLinked("Current Task");
			var taskLog = NodeCollection.FindNextLinked<HeaderNode>(currentTask?.Next ?? throw BadLogPage);
			if (taskLog == null)
			{
				throw BadLogPage;
			}

			var previousTask = WikiTextVisitor.Raw(NodeCollection.NodesBetween(currentTask, taskLog)).Trim().TrimEnd(TextArrays.Period);
			NodeCollection.RemoveBetween(currentTask, taskLog, false);
			nodes.AddAfter(currentTask, new TextNode("\n" + this.status + ".\n\n"));
			return previousTask == this.status;
		}

		private void UpdateEntry(Page sender, EventArgs eventArgs)
		{
			Debug.Assert(this.logInfo != null, "LogInfo is null.");
			var parsedText = WikiTextParser.Parse(sender.Text);
			var result = this.UpdateCurrentStatus(parsedText);
			var entryNode = parsedText.FindFirstLinked<TemplateNode>(template => WikiTextVisitor.Value(template.Title).Trim() == "/Entry");
			if (entryNode == null || !(entryNode.Value is TemplateNode entry))
			{
				// CONSIDER: This used to insert a /Entry into an empty table, but given that we're not currently parsing tables, that would've required far too much code for a one-off situation, so it's been left out. Could theoretically be reintroduced once table parsing is in place.
				throw BadLogPage;
			}

			// If the last job was the same as this job and has no end time, then it's either the current job or a resumed one.
			if (string.IsNullOrEmpty(entry.ValueOf("3")) &&
				entry.ValueOf("1")?.Trim() == this.logInfo.Title &&
				(entry.ValueOf("info") ?? string.Empty) == (this.logInfo.Details ?? string.Empty))
			{
				// If the end date is not null, then we're at the end of the job, so update the end time.
				if (this.end != null)
				{
					var start = entry.FindParameterLinked("2");
					Debug.Assert(start != null, "Start parameter not found.");
					var end = ParameterNode.FromParts(3, FormatDateTime(DateTime.UtcNow));
					entry.Parameters.AddAfter(start, end);
					sender.Text = WikiTextVisitor.Raw(parsedText);
				}

				return;
			}

			this.AddTemplateToPage(entryNode);
			sender.Text = WikiTextVisitor.Raw(parsedText);
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