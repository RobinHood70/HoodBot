namespace RobinHood70.Robby.Pages
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.WikiCommon;
	using WallE.Base;
	using static WikiCommon.Extensions;
	using static WikiCommon.Globals;

	public class FilePage : Page
	{
		#region Fields
		private List<FileRevision> fileRevisions = new List<FileRevision>();
		#endregion

		#region Constructors
		public FilePage(Site site, string title)
			: base(site, title)
		{
		}
		#endregion

		#region Public Properties
		public IReadOnlyCollection<FileRevision> FileRevisions => this.fileRevisions;

		public FileRevision LatestFileRevision { get; private set; }
		#endregion

		#region Public Methods
		public IEnumerable<string> FileUsage() => FileUsage(this.Site.Namespaces.Ids, Filter.All);

		public IEnumerable<string> FileUsage(IEnumerable<int> namespaces, Filter filterRedirects)
		{
			var propModule = new FileUsageInput
			{
				Namespaces = namespaces,
				FilterRedirects = filterRedirects
			};
			var pageSet = new PageSetInput(new[] { this.FullPageName });
			var result = this.Site.AbstractionLayer.LoadPages(pageSet, new[] { propModule });
			foreach (var usage in result.First().FileUsages)
			{
				yield return usage.Title;
			}
		}

		public IEnumerable<string> FindDuplicateFiles() => FindDuplicateFiles(true);

		public IEnumerable<string> FindDuplicateFiles(bool localOnly)
		{
			var propModule = new DuplicateFilesInput();
			var pageSet = new PageSetInput(new[] { this.FullPageName });
			var result = this.Site.AbstractionLayer.LoadPages(pageSet, new[] { propModule });
			foreach (var dupe in result.First().DuplicateFiles)
			{
				yield return dupe.Name;
			}
		}
		#endregion

		#region Protected Override Methods
		protected override void PopulateCustomResults(PageItem pageItem)
		{
			ThrowNull(pageItem, nameof(pageItem));
			this.fileRevisions.Clear();
			if (pageItem.ImageInfoEntries != null)
			{
				var latest = DateTime.MinValue;
				foreach (var imageInfoEntry in pageItem.ImageInfoEntries)
				{
					var fileRevision = new FileRevision(
						bitDepth: imageInfoEntry.BitDepth,
						fileSize: imageInfoEntry.Size,
						height: imageInfoEntry.Height,
						width: imageInfoEntry.Width,
						comment: imageInfoEntry.Comment,
						mimeType: imageInfoEntry.MimeType,
						user: imageInfoEntry.User,
						timestamp: imageInfoEntry.Timestamp,
						uri: new Uri(imageInfoEntry.Uri));
					this.fileRevisions.Add(fileRevision);

					if (fileRevision.Timestamp > latest)
					{
						this.LatestFileRevision = fileRevision;
						latest = fileRevision.Timestamp.Value;
					}
				}
			}
		}
		#endregion
	}
}