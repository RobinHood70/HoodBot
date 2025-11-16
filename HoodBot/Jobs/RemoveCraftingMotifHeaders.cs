namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Diagnostics;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Uesp;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon.Parser;

internal sealed class RemoveCraftingMotifHeaders : EditJob
{
	#region Constructors
	[JobInfo("Remove Crafting Motif Headers")]
	public RemoveCraftingMotifHeaders(JobManager jobManager)
		: base(jobManager)
	{
	}
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Remove Headers/Indentation";

	protected override void LoadPages()
	{
		this.Pages.GetNamespace(UespNamespaces.Online, Filter.Exclude, "Crafting Motif ");
		this.Pages.GetNamespace(UespNamespaces.Lore, Filter.Exclude, "Crafting Motif ");
	}

	protected override void PageLoaded(Page page)
	{
		// Crude unindent before parsing
		page.Text = page.Text.Replace("\r\n", "\n", StringComparison.Ordinal);
		page.Text = page.Title.PageName.StartsWith("Crafting Motif 68", StringComparison.Ordinal)
			? page.Text
				.Replace("\n\n:", "\n\n", StringComparison.Ordinal)
				.Replace("==\n:", "==\n", StringComparison.Ordinal)
				.Replace("}}\n:", "}}\n", StringComparison.Ordinal)
				.Replace("\n:", "<br>\n", StringComparison.Ordinal)
			: page.Text
				.Replace("\n\n:", "\n\n", StringComparison.Ordinal)
				.Replace("\n:", "\n\n", StringComparison.Ordinal);
		var parser = new SiteParser(page);

		// Double-check that this is actually a book (not currently necessary, but left as a failsafe)
		if (parser.FindTemplate("Game Book") is null && parser.FindTemplate("Lore Book") is null)
		{
			Debug.WriteLine(page.Title + " - no template");
			return;
		}

		RemoveHeaders(parser);
		parser.UpdatePage();
	}
	#endregion

	#region Private Static Methods
	private static void RemoveHeaders(SiteParser parser)
	{
		for (var i = 0; i < parser.Count; i++)
		{
			if (parser[i] is IHeaderNode header && header.Level == 4)
			{
				parser[i] = parser.Factory.TextNode(header.Title.ToRaw().Trim() + header.Comment.ToRaw().TrimEnd());
			}
		}
	}
	#endregion
}