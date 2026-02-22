namespace RobinHood70.HoodBot.Jobs;

using System.Diagnostics;
using RobinHood70.Robby;

[method: JobInfo("One-Off Job")]
internal sealed class OneOffJob(JobManager jobManager) : WikiJob(jobManager, JobType.Write)
{
	#region Fields
	private TitleCollection deletions = new(
		jobManager.Site,
		"Daggerfall:The Fey (NPC)",
		"Daggerfall:Whitka (NPC)",
		"Daggerfall:Thyr Topfield (NPC)",
		"Daggerfall:Thaik (NPC)",
		"Daggerfall:Sylch Greenwood (NPC)",
		"Daggerfall:Squid (NPC)",
		"Daggerfall:Skakmat (NPC)",
		"Daggerfall:Lord Bertram Spode (NPC)",
		"Daggerfall:The Crow (NPC)",
		"Daggerfall:Baltham Greyman (NPC)",
		"Daggerfall:Lord Kilbar (NPC)",
		"Daggerfall:Farrington (NPC)",
		"Daggerfall:Lord Perwright (NPC)",
		"Daggerfall:Lord Coulder (NPC)",
		"Daggerfall:Lord Plessington (NPC)",
		"Daggerfall:Lord Quistley (NPC)",
		"Daggerfall:Lord Kain (NPC)",
		"Daggerfall:Lord Harth (NPC)",
		"Daggerfall:The Acolyte (NPC)",
		"Daggerfall:The Oracle (NPC)",
		"Daggerfall:Greklith (NPC)",
		"Daggerfall:Archmagister (NPC)",
		"Daggerfall:Lord Woodborne (NPC)",
		"Daggerfall:Br'itsa (NPC)",
		"Daggerfall:Lord Darkworth (NPC)",
		"Daggerfall:The Underking (NPC)",
		"Daggerfall:The Night Mother (NPC)",
		"Daggerfall:Chulmore Quill (NPC)",
		"Daggerfall:Mobar (NPC)",
		"Daggerfall:Baroness Dh'emka (NPC)",
		"Daggerfall:Charvek-si (NPC)",
		"Daggerfall:Karethys (NPC)",
		"Daggerfall:Popudax (NPC)",
		"Daggerfall:Lord Provlith (NPC)",
		"Daggerfall:The Great Knight (NPC)",
		"Daggerfall:Lady Brisienna (NPC)",
		"Daggerfall:Lord Castellian (NPC)",
		"Daggerfall:Baron Shrike (NPC)",
		"Daggerfall:Lady Northbridge (NPC)",
		"Daggerfall:Lady Flyte (NPC)",
		"Daggerfall:Lord Flyte (NPC)",
		"Daggerfall:Lord K'avar (NPC)",
		"Daggerfall:Lord Vhosek (NPC)",
		"Daggerfall:Lady Bridwell (NPC)",
		"Daggerfall:Lord Bridwell (NPC)",
		"Daggerfall:Cyndassa (NPC)",
		"Daggerfall:Nulfaga (NPC)",
		"Daggerfall:Lhotun (NPC)",
		"Daggerfall:Gothryd (NPC)",
		"Daggerfall:Gortwog (NPC)",
		"Daggerfall:Mynisera (NPC)",
		"Daggerfall:Medora Direnni (NPC)",
		"Daggerfall:Akorithi (NPC)",
		"Daggerfall:Aubk-i (NPC)",
		"Daggerfall:Elysana (NPC)",
		"Daggerfall:Morgiah (NPC)",
		"Daggerfall:Helseth (NPC)",
		"Daggerfall:Eadwyre (NPC)");
	#endregion

	#region Protected Override Methods
	protected override void Main()
	{
		this.deletions.Sort();
		foreach (var title in this.deletions)
		{
			var titlePage = this.Site.LoadPage(title);
			if (titlePage.IsMissing)
			{
				continue;
			}

			var titleMain = title.FullPageName()[..^6];
			var titleMainPage = this.Site.LoadPage(titleMain);
			if (titleMainPage.Text.Contains("{{NPC Summary", System.StringComparison.Ordinal))
			{
				title.Delete("Information copied to main NPC page");
			}
			else
			{
				Debug.Write("WTF?");
			}
		}
	}
	#endregion
}