namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Jobs.JobModels;
using RobinHood70.HoodBot.Models;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

internal sealed class EsoBoundTooltipNote : ParsedPageJob
{
	#region Private Constants
	private const string NoteTemplate = "Online Bound Tooltip Note";
	#endregion

	#region Static Fields
	private static readonly HashSet<string> AfterNotes = new(StringComparer.Ordinal) { "References", "Bugs", "Gallery" };
	#endregion

	#region Private Fields
	private readonly Title newLeft;
	#endregion

	#region Constructors
	[JobInfo("Bound Tooltip Note")]
	public EsoBoundTooltipNote(JobManager jobManager)
		: base(jobManager)
	{
		this.newLeft = new Title(this.Site, MediaWikiNamespaces.Template, "NewLeft");
		//// this.Shuffle = true;
	}
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Add bound tooltip note";

	protected override void LoadPages() => this.Pages.GetBacklinks("Template:Online Furnishing Purchase", BacklinksTypes.EmbeddedIn, true, Filter.Exclude);

	protected override void PageLoaded(Page page)
	{
		page.Text = page.Text.Replace("Although this furnishing may not have a '''Bound''' tooltip, it will become Bound to account after purchase from the [[Online:Crown Store|Crown Store]], [[Online:Housing Editor|Housing Editor]], or a [[Online:Crown Store/Furniture/Thematic Bundles|Furnishing Bundle]].", "{{" + NoteTemplate + "}}", StringComparison.OrdinalIgnoreCase);
		base.PageLoaded(page);
	}

	protected override void ParseText(SiteParser parser)
	{
		var summaryTemplate = parser.FindTemplate("Online Furnishing Summary") ?? throw new InvalidOperationException();
		var purchaseTemplate = parser.FindTemplate("Online Furnishing Purchase");

		if (!IsValidPage(summaryTemplate, purchaseTemplate))
		{
			return;
		}

#if DEBUG
		if (CheckMultipleNotes(parser) > 1)
		{
			throw new InvalidOperationException(parser.Title + ": too many Notes sections");
		}
#endif

		RemoveNotesComment(parser);
		this.AddNote(parser);
		summaryTemplate.Update("bindtype", "Bound On Purchase", ParameterFormat.OnePerLine, false);
	}
	#endregion

	#region Private Static Methods
	private static int CheckMultipleNotes(SiteParser parser)
	{
		var commentCount = 0;
		foreach (var node in parser)
		{
			switch (node)
			{
				case IHeaderNode headerNode:
					if (headerNode.Level == 2 && (headerNode.GetTitle(true)?.OrdinalEquals("Notes") ?? false))
					{
						commentCount++;
					}

					break;
				case ICommentNode commentNode:
					var matches = WikiRegexes.HeaderFinder().Matches(commentNode.Comment);
					foreach (Match match in matches)
					{
						if (match.Groups["title"].Value.OrdinalEquals("Notes"))
						{
							commentCount++;
						}
					}

					break;
				default:
					break;
			}
		}

		return commentCount;
	}

	private static void FixupPrevSection(WikiNodeCollection prevContent)
	{
		switch (prevContent[^1])
		{
			case ITextNode text:
				if (text.Text[^1] != '\n')
				{
					prevContent.AddText("\n");
				}

				break;
			case ICommentNode comment:
				if (comment.Comment[^1] != '\n')
				{
					prevContent.AddText("\n");
				}

				break;
			default:
				prevContent.AddText("\n");
				break;
		}
	}

	private static bool IsValidPage(ITemplateNode summaryTemplate, ITemplateNode? purchaseTemplate)
	{
		var bindType = summaryTemplate.Find("bindtype");
		if (!bindType.IsNullOrWhitespace())
		{
			return false;
		}

		var crowns = summaryTemplate.GetValue("vendorcrowns") ?? purchaseTemplate?.GetValue("vendorcrowns");
		if (crowns.OrdinalEquals("Housing Editor"))
		{
			return true;
		}

		var bundles = summaryTemplate.Find("bundles") ?? purchaseTemplate?.Find("bundles");
		return !bundles.IsNullOrWhitespace();
	}

	private static void RemoveNotesComment(SiteParser parser)
	{
		if (parser.Find<IHeaderNode>(headerNode => headerNode.Level == 2 && (headerNode.GetTitle(true)?.OrdinalEquals("Notes") ?? false)) is not null)
		{
			return;
		}

		var notesIndex = parser.FindIndex(n => n is ICommentNode c && WikiRegexes.HeaderFinder().Match(c.Comment).Groups["title"].Value.OrdinalEquals("Notes"));
		if (notesIndex == -1)
		{
			return;
		}

		var comment = (ICommentNode)parser[notesIndex];
		if (notesIndex > 0 && parser[notesIndex - 1] is ICommentNode instructions && instructions.Comment.StartsWith("<!--Instructions: ", StringComparison.Ordinal))
		{
			parser.RemoveRange(notesIndex - 1, 2);
			notesIndex--;
		}
		else
		{
			parser.RemoveAt(notesIndex);
		}

		/*
		var notes = comment.Comment[4..]
			.Replace("-->", string.Empty, StringComparison.Ordinal)
			.Trim()
			.TrimEnd('*');
		parser.InsertParsed(notesIndex, notes);
		*/
	}
	#endregion

	#region Private Methods
	private void AddNote(SiteParser parser)
	{
		var sections = parser.ToSections();
		var index = sections.FindIndex(s => s.Header?.Level == 2 && (s.GetTitle()?.OrdinalEquals("Notes") ?? false));
		Section notes;
		string text;
		if (index == -1)
		{
			text = "\n\n";
			notes = Section.FromText(parser.Factory, "Notes", text);
			index = this.InsertSection(sections, notes);
		}
		else
		{
			notes = sections[index];
			text = notes.Content.ToRaw();

			// Only look for partial match in case spacing or something else has changed for some reason.
			if (notes.Content.FindTemplate(this.Site, NoteTemplate) is not null)
			{
				return;
			}
		}

		FixupPrevSection(sections[index - 1].Content);
		text = "\n{{" + NoteTemplate + "}}" + text;
		notes.Content.Clear();
		notes.Content.AddParsed(text);
		parser.FromSections(sections);
	}

	private int InsertSection(SectionCollection sections, Section notes)
	{
		var insertAt = sections.Count;
		for (var i = sections.Count - 1; i >= 1; i--)
		{
			var title = sections[i].GetTitle();
			if (title is not null && AfterNotes.Contains(title))
			{
				insertAt = i;
			}
		}

		sections.Insert(insertAt, notes);
		var prevContent = sections[insertAt - 1].Content;
		FixupPrevSection(prevContent);
		if (prevContent.FindTemplate(this.newLeft) is null)
		{
			prevContent.AddParsed("{{NewLeft}}\n");
		}

		return insertAt;
	}
	#endregion
}