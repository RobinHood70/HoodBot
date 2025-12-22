namespace RobinHood70.HoodBot.Jobs;

using System.Diagnostics;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;

[method: JobInfo("Check File Licensing")]
internal sealed class CheckFileLicensing(JobManager jobManager) : WikiJob(jobManager, JobType.ReadOnly)
{
	#region Protected Override Methods
	protected override void Main()
	{
		var licenseTemplates = this.GetImageLicenses();
		var filePages = this.GetFilePages();
		foreach (var filePage in filePages)
		{
			var parser = new SiteParser(filePage);
			if (!this.HasLicense(licenseTemplates, parser))
			{
				Debug.WriteLine($"No license: {filePage.Title}");
			}
		}
	}
	#endregion

	#region Private Methods
	private TitleCollection GetImageLicenses() => new TitleCollection(this.Site).GetCategoryMembers("Image Copyright Templates");

	private PageCollection GetFilePages()
	{
		var retval = new PageCollection(this.Site, PageModules.Default, false);
		retval.SetLimitations(LimitationType.OnlyAllow, MediaWikiNamespaces.File);
		retval.GetNamespace(MediaWikiNamespaces.File, Filter.Exclude, "OB-");
		retval.Sort();

		return retval;
	}

	private bool HasLicense(TitleCollection licenseTemplates, SiteParser parser)
	{
		foreach (var template in parser.TemplateNodes)
		{
			var templateTitle = template.GetTitle(this.Site);
			if (licenseTemplates.Contains(templateTitle))
			{
				return true;
			}
		}

		return false;
	}
	#endregion
}