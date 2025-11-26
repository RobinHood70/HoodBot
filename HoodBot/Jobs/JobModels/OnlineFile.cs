namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.WikiCommon.Parser;

// Saved here for now as likely to be of use in various classes in the future.
public sealed class OnlineFile
{
	#region Public Properties
	public DateTime? CreationTime { get; private set; }

	public string? Description { get; private set; }

	public bool NoSummary { get; private set; }

	public SortedSet<string> OriginalFiles { get; } = [];

	public IList<Title> Renames { get; } = [];

	public Title? Title { get; private set; }

	public SortedSet<(string ItemType, string Text)> Uses { get; } = [];
	#endregion

	#region Public Methods
	public void MergeInfo(FilePage filePage, ITemplateNode template)
	{
		if (filePage.FileRevisions.Count == 0)
		{
			throw new InvalidOperationException("No file revisions, or creation time not found.");
		}

		if (template.GetValue("originalfile") is string origFiles)
		{
			this.OriginalFiles.UnionWith(origFiles.Split(TextArrays.Comma, StringSplitOptions.TrimEntries));
		}

		foreach (var paramSet in template.ParameterCluster(2))
		{
			var itemType = paramSet[0].ToRaw();
			var text = paramSet[1].ToRaw();
			this.Uses.Add((itemType, text));
		}

		var isOlder = false;
		foreach (var fileRev in filePage.FileRevisions)
		{
			if (fileRev.Timestamp is DateTime revTimestamp && revTimestamp < this.CreationTime)
			{
				isOlder = true;
				this.CreationTime = revTimestamp;
			}
		}

		if (this.Title is null)
		{
			this.Title = filePage.Title;
		}
		else if (isOlder)
		{
			this.Renames.Add(this.Title);
			this.Title = filePage.Title;
		}
		else
		{
			this.Renames.Add(filePage.Title);
		}

		if (isOlder || this.Description is null)
		{
			this.Description = template.GetValue("description");
		}

		this.NoSummary = !this.NoSummary && !string.IsNullOrWhiteSpace(template.GetValue("nosummary"));
	}
	#endregion
}