namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.IO;
using RobinHood70.HoodBot.Jobs.JobModels;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon.Parser;

public class FixOriginalFiles : TemplateJob
{
	// Note: this class is relatively slow, since it loads all pages with {{Online File}}. It could be made faster by only loading the originalfile variable and validating that, then loading only the pages that need fixed. This would, however, add a fair bit of complexity. Since this isn't likely to be a frequently run job, I've opted for simplicity for now.
	#region Fields
	private readonly Dictionary<string, string> iconNames = new(StringComparer.OrdinalIgnoreCase);
	#endregion

	#region Constructors

	[JobInfo("Fix originalfile names")]
	public FixOriginalFiles(JobManager jobManager)
		: base(jobManager)
	{
		this.Shuffle = true;
	}
	#endregion

	#region Public Override Properties
	public override string LogDetails => "Update " + this.TemplateName;

	public override string LogName => "Fix originalfile parameter";
	#endregion

	#region Protected Override Properties
	protected override string TemplateName => "Online File";
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Fix originalfile";

	protected override void LoadPages()
	{
		// Note: since this job scans all files, GetIcons is currently disabled. It's expected that the various icon folders will have been updated manually, based on true need rather than the bad guess that GetIcons makes.

		// EsoSpace.GetIcons(this, EsoLog.LatestDBUpdate(false));
		var dupes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var folderLen = LocalConfig.EsoUIFolder.Length + 1;
		foreach (var fullName in Directory.EnumerateFiles(LocalConfig.EsoUIFolder, "*.png", SearchOption.AllDirectories))
		{
			var file = Path.GetFileNameWithoutExtension(fullName);
			var dir = Path.GetDirectoryName(fullName)!;
			dir = (dir.Length >= folderLen) ? dir[folderLen..].Replace('\\', '/') : string.Empty;

			if (!dupes.Contains(file) &&
				!this.iconNames.TryAdd(file, dir))
			{
				dupes.Add(file);
				this.iconNames.Remove(file);
			}
		}

		base.LoadPages();
	}

	protected override void ParseTemplate(ITemplateNode template, SiteParser parser)
	{
		if (GetParameter(template) is not IParameterNode filename)
		{
			throw new InvalidOperationException("originalfile not found: " + parser.Page.Title.PageName);
		}

		var paramValue = Sanitize(filename.GetRaw());
		if (HasSquareBrackets(parser.Title, paramValue))
		{
			this.StatusWriteLine("Possibly malformed originalfile: " + parser.Title.FullPageName());
		}

		if (!paramValue.Contains('/', StringComparison.Ordinal))
		{
			if (this.iconNames.TryGetValue(paramValue, out var dir))
			{
				paramValue = $"{dir}/{paramValue}";
			}
			else if (!parser.Title.PageName.Contains("-map-", StringComparison.OrdinalIgnoreCase))
			{
				this.StatusWriteLine($"Could not validate icon name {paramValue} on page {parser.Title.FullPageName()}");
			}
		}

		filename.SetValue(paramValue, ParameterFormat.Copy);
	}
	#endregion

	#region Private Static Methods
	private static IParameterNode? GetParameter(ITemplateNode template)
	{
		// Checks for legitimate value first, then handles possible malformations.
		if (template.Find("originalfile") is IParameterNode retval)
		{
			return retval;
		}

		if ((template.Parameters.Count & 1) == 1 && template.Find(1) is IParameterNode misplaced)
		{
			misplaced.SetName("originalfile");
			return misplaced;
		}

		return null;
	}

	private static bool HasSquareBrackets(Title title, string paramValue) =>
		!title.PageNameEquals("ON-icon-store-Sunken_Trove_Crown_Crate.png") &&
		(paramValue.Contains('[', StringComparison.Ordinal) ||
		paramValue.Contains(']', StringComparison.Ordinal));

	private static string Sanitize(string paramValue)
	{
		paramValue = paramValue.Replace("<br>", string.Empty, StringComparison.OrdinalIgnoreCase);
		if (paramValue.Length > 0 && paramValue[0] == '/')
		{
			paramValue = paramValue[1..];
		}

		if (paramValue.StartsWith("esoui/art/", StringComparison.OrdinalIgnoreCase))
		{
			paramValue = paramValue[10..];
		}

		var split = paramValue.Split('.', 2);
		var ext = split.Length > 1 ? split[1] : string.Empty;
		if (string.Equals(ext, "png", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(ext, "dds", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(ext, "jpg", StringComparison.OrdinalIgnoreCase))
		{
			paramValue = split[0];
		}

		return paramValue;
	}
	#endregion
}