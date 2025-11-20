namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.WikiCommon.Parser;

// Saved here for now as likely to be of use in various classes in the future.
public sealed class OnlineFile
{
	#region Public Properties
	public DateTime CreationTime { get; private set; } = DateTime.MinValue;

	public string? Description { get; private set; }

	public bool NoSummary { get; private set; }

	public SortedSet<string> OriginalFiles { get; } = [];

	public IList<Title> Renames { get; } = [];

	public Title? Title { get; private set; }

	public SortedSet<(string ItemType, string Text)> Uses { get; } = [];
	#endregion

	#region Public Methods
	public void MergeInfo(FilePage page, ITemplateNode template)
	{
		foreach (var paramSet in template.ParameterCluster(2))
		{
			var itemType = paramSet[0].ToRaw();
			var text = paramSet[1].ToRaw();
			this.Uses.Add((itemType, text));
		}

		if (page.FileRevisions.Count > 1)
		{
			Debug.WriteLine(page.Title.ToString() + ": " + page.FileRevisions.Count.ToStringInvariant());
		}

		var creationTime = DateTime.MaxValue;
		foreach (var fileRev in page.FileRevisions)
		{
			if (fileRev.Timestamp is DateTime revTimestamp && revTimestamp < creationTime)
			{
				creationTime = revTimestamp;
			}
		}

		if (creationTime == DateTime.MaxValue)
		{
			throw new InvalidOperationException("No file revisions, or creation time not found.");
		}

		this.CreationTime = creationTime;
		this.Description = template.GetValue("description");
		this.NoSummary = !string.IsNullOrWhiteSpace(template.GetValue("nosummary"));
		if (template.GetValue("originalfile")?.Split(TextArrays.Comma) is string[] origFiles)
		{
			this.OriginalFiles.AddRange(origFiles.Select(s => s.Trim()));
		}

		if (this.Title is not null)
		{
			this.Renames.Add(this.Title);
		}

		this.Title = page.Title;
	}
	#endregion
}