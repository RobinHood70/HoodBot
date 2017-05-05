namespace RobinHood70.Robby.Pages
{
	using System;
	using System.Collections.Generic;
	using Design;
	using WallE.Base;
	using static WikiCommon.Globals;

	public class FilePage : Page
	{
		private List<FileRevision> fileRevisions = new List<FileRevision>();

		public FilePage(Site site, string title, PageLoadOptions loadOptions)
			: base(site, title, loadOptions)
		{
		}

		public IReadOnlyCollection<FileRevision> FileRevisions => this.fileRevisions;

		public FileRevision LatestFileRevision { get; private set; }

		public override void Populate(PageItem pageItem)
		{
			ThrowNull(pageItem, nameof(pageItem));
			base.Populate(pageItem);

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
	}
}