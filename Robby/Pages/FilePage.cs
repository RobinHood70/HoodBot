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
		public IReadOnlyList<string> FileUsage() => FileUsage(this.Site.Namespaces.RegularIds, Filter.Any);

		public IReadOnlyList<string> FileUsage(IEnumerable<int> namespaces, Filter filterRedirects)
		{
			var propModule = new FileUsageInput
			{
				Namespaces = namespaces,
				FilterRedirects = filterRedirects
			};
			var pageSet = new PageSetInput(new[] { this.FullPageName });
			var result = this.Site.AbstractionLayer.LoadPages(pageSet, new[] { propModule });
			var page = result.First();
			var retval = new List<string>(page.FileUsages.Count);
			foreach (var usage in page.FileUsages)
			{
				retval.Add(usage.Title);
			}

			return retval.AsReadOnly();
		}

		public IReadOnlyList<string> FindDuplicateFiles() => FindDuplicateFiles(true);

		public IReadOnlyList<string> FindDuplicateFiles(bool localOnly)
		{
			var propModule = new DuplicateFilesInput() { LocalOnly = localOnly };
			var pageSet = new PageSetInput(new[] { this.FullPageName });
			var result = this.Site.AbstractionLayer.LoadPages(pageSet, new[] { propModule });
			var page = result.First();
			var retval = new List<string>(page.DuplicateFiles.Count);
			foreach (var dupe in page.DuplicateFiles)
			{
				retval.Add(dupe.Name);
			}

			return retval.AsReadOnly();
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