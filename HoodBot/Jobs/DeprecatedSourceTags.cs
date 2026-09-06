namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon.Parser;
using RobinHood70.WikiCommon.Parser.Basic;

internal sealed partial class DeprecatedSourceTags : EditJob
{
	#region Fields
	private readonly ICollection<string> allAttribs = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
	private readonly WikiNodeFactory factory = new();
	#endregion

	#region Constructors
	[JobInfo("Deptrecated Source Tags")]
	public DeprecatedSourceTags(JobManager jobManager)
		: base(jobManager)
	{
		this.Pages.SetLimitations(Robby.Design.LimitationType.None);
		this.factory.UnparsedTags.Add("audio");
		this.factory.UnparsedTags.Add("source");
		this.factory.UnparsedTags.Add("syntaxhighlight");
		this.factory.UnparsedTags.Add("video");
	}
	#endregion

	#region Private Properties
	[GeneratedRegex(@"\s+(?<name>[\w\-.:]+)(\s*=\s*(?:""(?<value>[^""]*)""|'(?<value>[^']*)'|(?<value>[^\s""'>]+)))?", RegexOptions.None)]
	private static partial Regex HtmlAttribute { get; }
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Convert source tags to syntaxhighlight";

	protected override void LoadPages() => this.Pages.GetCategoryMembers("Pages using deprecated source tags");

	protected override void PageLoaded(Page page)
	{
		var parser = new SiteParser(page, page.Text, InclusionType.Raw, false, this.factory);
		foreach (var node in parser)
		{
			if (node is TagNode tag && tag.Name.OrdinalICEquals("source"))
			{
				var hasLang = this.ParseSourceTag(tag);
				if (!hasLang)
				{
					this.StatusWriteLine("No lang in tag: " + page.Title);
				}

				tag.Name = "syntaxhighlight";
				tag.Close = "</syntaxhighlight>";
			}
		}

		parser.UpdatePage();
	}

	private bool ParseSourceTag(TagNode tag)
	{
		if (tag.Attributes is not string attribText)
		{
			return false;
		}

		var hasLang = false;
		var attribs = (IReadOnlyList<Match>)HtmlAttribute.Matches(attribText);
		var reconstructed = new StringBuilder(attribText.Length);
		foreach (var attrib in attribs)
		{
			var attribName = attrib.Groups["name"].Value.TrimStart();
			var attribValue = attrib.Groups["value"].Value;
			var append = attrib.Groups[1].Value.TrimStart(['=', ' ', '\t', '\n', '\r', '\f']);
			append = append.Length > 0 ? attribName + '=' + append : attribName;
			switch (attribName)
			{
				case "enclose":
					append = attribValue.OrdinalICEquals("none")
						? " inline"
						: string.Empty;
					break;
				case "lang":
					hasLang = true;
					break;
				case "start":
					if (int.TryParse(attribValue, CultureInfo.InvariantCulture, out var startAt) &&
						startAt <= 1)
					{
						append = string.Empty;
					}

					break;
				default:
					break;
			}

			reconstructed.Append(append);
			this.allAttribs.Add(append);
		}

		attribText = reconstructed.ToString().Trim();
		if (attribText.Length != 0)
		{
			tag.Attributes = ' ' + attribText;
		}

		return hasLang;
	}
	#endregion
}