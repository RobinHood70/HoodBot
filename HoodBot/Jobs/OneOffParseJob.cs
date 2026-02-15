namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Diagnostics;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

[method: JobInfo("One-Off Parse Job")]
internal sealed class OneOffParseJob(JobManager jobManager) : ParsedPageJob(jobManager)
{
	#region Private Constants
	private const string EndText = "[[Online:Furnishings/Luxury Furnisher|Luxury Furnishings]]";
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Update section text";

	protected override void LoadPages() => this.Pages.GetBacklinks("Online:Zanil Theran", BacklinksTypes.Backlinks);

	protected override void ParseText(SiteParser parser)
	{
		var sections = parser.ToSections();
		var section = sections.FindFirst("Available From");
		if (section is null)
		{
			return;
		}

		var text = section.Content.ToRaw();
		var offset = text.IndexOf(EndText, StringComparison.Ordinal);
		if (offset == -1)
		{
			Debug.WriteLine("Couldn't find end text on " + parser.Title);
			return;
		}

		if (text[offset + EndText.Length] == '.')
		{
			offset++;
		}

		text = text[(offset + EndText.Length)..];
		text = "\n* This furnishing is a '''Luxury''' item. Luxury items are on a 'week/year' rotation. Once a week, the [[Online:Luxury Furnisher|Luxury Furnisher]] appears with a set of wares which rotate each week, and ultimately, may not be seen again for one year.<br>There are a few exceptions to the rotation, given that there may be events and new releases that disrupt the usual ware list for a week.\n* Visit [[Online:Zanil Theran|Zanil Theran]] in [[Online:Coldharbour|Coldharbour]]'s [[Online:The Hollow City|The Hollow City]] or in [[Online:Craglorn|Craglorn]] at the [[Online:Belkarth Festival Grounds|Belkarth Festival Grounds]] every Friday night after 8:00 PM ET to view the weekly [[Online:Furnishings/Luxury Furnisher|Luxury Furnishings]]." + text;
		section.Content.Clear();
		section.Content.AddText(text);
		parser.FromSections(sections);
	}
	#endregion
}