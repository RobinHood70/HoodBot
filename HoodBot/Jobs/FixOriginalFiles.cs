namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.IO;
using RobinHood70.HoodBot.Jobs.JobModels;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon.Parser;

[method: JobInfo("Fix originalfile names", "ESO")]
public class FixOriginalFiles(JobManager jobManager) : TemplateJob(jobManager)
{
	// Note: this class is relatively slow, since it loads all pages with {{Online File}}. It could be made faster by only loading the originalfile variable and validating that, then loading only the pages that need fixed. This would, however, add a fair bit of complexity. Since this isn't likely to be a frequently run job, I've opted for simplicity for now.
	#region Fields
	private readonly Dictionary<string, string> iconHashes = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, string> iconNames = new(StringComparer.OrdinalIgnoreCase);
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
		this.DownloadEsoFile(EsoFileTypes.Icons);
		var dupes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var folderLen = LocalConfig.EsoUIArtFolder.Length + 1;
		foreach (var fullName in Directory.EnumerateFiles(LocalConfig.EsoUIArtFolder, "*.png", SearchOption.AllDirectories))
		{
			var file = Path.GetFileNameWithoutExtension(fullName);
			var dir = Path.GetDirectoryName(fullName)!;
			dir = (dir.Length >= folderLen) ? dir[folderLen..].Replace('\\', '/') : string.Empty;

			if (!dupes.Contains(file))
			{
				if (this.iconNames.TryAdd(file, dir))
				{
					this.iconHashes.Add(file, EsoFiles.CalculateHash(fullName));
				}
				else
				{
					dupes.Add(file);
					this.iconNames.Remove(file);
				}
			}
		}

		base.LoadPages();
	}

	protected override void ParseTemplate(ITemplateNode template, SiteParser parser)
	{
		if (GetParameter(template) is not IParameterNode fileName)
		{
			throw new InvalidOperationException("originalfile not found: " + parser.Page.Title.PageName);
		}

		var paramValue = EsoFiles.SanitizeFileName(fileName.GetRaw());
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

		fileName.SetValue(paramValue, ParameterFormat.Copy);
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
	#endregion
}