namespace RobinHood70.HoodBot.Jobs.Loggers;

using System;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Properties;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;
using RobinHood70.WallE.Design;
using RobinHood70.WikiCommon.Parser;

public class PageJobLogger : JobLogger
{
	#region Static Fields
	private static readonly InvalidOperationException BadLogPage = new(Resources.BadLogPage);
	#endregion

	#region Fields
	private readonly string clearStatus;
	private readonly string currentTaskTitle;
	private readonly string logPageLead;
	private readonly Title logTitle;
	private readonly string taskLogTitle;
	private DateTime? end;
	private LogInfo? logInfo;
	private Page? logPage;
	private DateTime? start;
	private string status;
	#endregion

	#region Constructors
	public PageJobLogger(Title logTitle)
	{
		// For now, log page translations are stored in HoodBot's resource files. If this becomes more complex in the future, or uses more languages than HoodBot itself supports (currently English and French), translations should be moved to their own resx files. All translations are here in the constructor.
		ArgumentNullException.ThrowIfNull(logTitle);
		var rm = new ResourceManager(typeof(Resources));
		var culture = logTitle.Site.Culture;
		this.clearStatus = rm.GetString("TaskNone", culture) ?? "None";
		this.currentTaskTitle = rm.GetString("CurrentTask", culture) ?? "Current Task";
		this.logPageLead = rm.GetString("LogPageLead", culture) ?? string.Empty;
		this.taskLogTitle = rm.GetString("TaskLog", culture) ?? "Task Log";
		this.logTitle = logTitle;
		this.status = this.clearStatus;
	}
	#endregion

	#region Public Override Methods
	public override void AddLogEntry(LogInfo info)
	{
		ArgumentNullException.ThrowIfNull(info);
		this.logInfo = info;
		this.start = DateTime.UtcNow;
		this.end = null;
		this.status = info.Title;
		this.UpdateEntry();
		this.SaveLogPage("Job Started", this.UpdateEntry);
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

	public override void EndLogEntry()
	{
		this.end = DateTime.UtcNow;
		this.status = this.clearStatus;
		this.UpdateEntry();
	}
	#endregion

	#region Private Static Methods
	private static void AddDateTime(ITemplateNode template, DateTime? dateTime)
	{
		if (dateTime.HasValue)
		{
			template.Add(FormatDateTime(dateTime.Value));
		}
	}

	private static string FormatDateTime(DateTime dt) => dt.ToString("u", CultureInfo.InvariantCulture).TrimEnd('Z');

	private static bool UpdateCurrentStatus(Section currentTask, string status)
	{
		var previousTask = currentTask.Content.ToRaw().Trim().TrimEnd(TextArrays.Period);
		currentTask.Content.Clear();
		currentTask.Content.AddText("\n" + status + ".\n\n");

		return previousTask.OrdinalEquals(status);
	}
	#endregion

	#region Private Methods
	private void AddParameters(ITemplateNode template, Section taskLog, int firstEntry)
	{
		template.Add(this.logInfo!.Title);
		if (!string.IsNullOrEmpty(this.logInfo.Details))
		{
			template.Add("info", this.logInfo.Details);
		}

		AddDateTime(template, this.start);
		AddDateTime(template, this.end);

		if (firstEntry == -1)
		{
			var text = taskLog.Content.ToRaw();
			text = text.Replace("|}", template.ToRaw() + "\n|}", StringComparison.Ordinal);
			taskLog.Content.Clear();
			taskLog.Content.AddText(text);
		}
		else
		{
			taskLog.Content.InsertRange(firstEntry, [template, template.Factory.TextNode("\n")]);
		}
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

	private void UpdateEntry()
	{
		Debug.Assert(this.logInfo != null, "LogInfo is null.");
		this.logPage ??= this.logTitle.Load();
		if (this.logPage.Text.Length == 0)
		{
			this.logPage.Text =
				"<cleanspace>\n" +
				"{{#local:hidetime|{{#expr:({{#time:Y}}*12+{{#time:m}}-{{#arg:months|1}})-1}}}}\n" +
				"{{#local:year|{{#expr:trunc(({{{hidetime}}})/12)}}}}\n" +
				"{{#local:month|{{padleft:{{#expr:({{{hidetime}}} mod 12)+1}}|2}}}}\n" +
				"{{#local:hidetime|{{{year}}}{{{month}}}}}\n" +
				"</cleanspace>" + this.logPageLead + '\n' +
				"\n" +
				"== " + this.currentTaskTitle + " ==\n" +
				this.status + ".\n" +
				"\n" +
				"== " + this.taskLogTitle + " ==\n" +
				"{| class=\"center\" style=\"white-space:nowrap; border-collapse:collapse;\"\n" +
				"|}";
		}

		SiteParser parser = new(this.logPage);
		var factory = parser.Factory;
		var sections = parser.ToSections(2);
		var currentTask = sections.FindFirst(this.currentTaskTitle) ?? throw BadLogPage;
		var taskLog = sections.FindFirst(this.taskLogTitle) ?? throw BadLogPage;
		var sameTaskText = UpdateCurrentStatus(currentTask, this.status);
		var firstEntry = taskLog.Content.IndexOf<ITemplateNode>(template => template.GetTitle(parser.Site).PageNameEquals("/Entry"));
		if (firstEntry != -1)
		{
			var entry = (ITemplateNode)taskLog.Content[firstEntry];
			if (this.end == null &&
				sameTaskText &&
				string.IsNullOrEmpty(entry.GetValue(3)) &&
				this.logInfo.Title.OrdinalEquals(entry.GetValue(1)) &&
				this.logInfo.Details.OrdinalEquals(entry.GetValue("info") ?? string.Empty))
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
					parser.FromSections(sections);
					parser.UpdatePage();
					return;
				}
			}
		}

		var template = parser.Factory.TemplateNodeFromParts("/Entry");
		this.AddParameters(template, taskLog, firstEntry);
		parser.FromSections(sections);
		parser.UpdatePage();
	}
	#endregion
}