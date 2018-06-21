﻿namespace RobinHood70.Robby.Pages
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using WallE.Base;
	using WikiCommon;
	using static WikiCommon.Globals;

	/// <summary>Represents a file on the wiki. Includes all page data as well as file revisions and file-specific methods.</summary>
	/// <seealso cref="RobinHood70.Robby.Pages.Page" />
	public class FilePage : Page
	{
		#region Fields
		private readonly List<FileRevision> fileRevisions = new List<FileRevision>();
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="FilePage"/> class.</summary>
		/// <param name="site">The site the file is from.</param>
		/// <param name="pageName">The page name (<em>without</em> the leading namespace).</param>
		public FilePage(Site site, string pageName)
			: base(site, MediaWikiNamespaces.File, pageName)
		{
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the list of file revisions.</summary>
		/// <value>The file revisions.</value>
		public IReadOnlyList<FileRevision> FileRevisions => this.fileRevisions;

		/// <summary>Gets the latest file revision.</summary>
		/// <value>The latest file revision.</value>
		public FileRevision LatestFileRevision { get; private set; }
		#endregion

		#region Public Methods

		/// <summary>Downloads the file to the specified location.</summary>
		/// <param name="fileName">Name of the file to download to. This will be overwritten if it exists. If fileName represents a path, the page name will be used as the file name.</param>
		public void Download(string fileName)
		{
			if (this.fileRevisions.Count == 0)
			{
				return;
			}

			try
			{
				var attributes = File.GetAttributes(fileName);
				if (attributes.HasFlag(FileAttributes.Directory))
				{
					fileName = Path.Combine(fileName, this.PageName);
				}
			}
			catch (FileNotFoundException)
			{
			}

			this.Site.Download(this.fileRevisions[0].Uri.OriginalString, fileName);
		}

		/// <summary>Finds all pages the file is used on.</summary>
		/// <returns>A list of <see cref="T:RobinHood70.Robby.Title"/>s that the file is used on.</returns>
		public TitleCollection FileUsage() => this.FileUsage(this.Site.Namespaces.RegularIds, Filter.Any);

		/// <summary>Finds all pages the file is used on within the given namespaces and optionally filters out redirects.</summary>
		/// <param name="namespaces">The namespaces to search.</param>
		/// <param name="filterRedirects">Filter redirects out of the result set.</param>
		/// <returns>A list of <see cref="T:RobinHood70.Robby.Title"/>s that the file is used on.</returns>
		public TitleCollection FileUsage(IEnumerable<int> namespaces, Filter filterRedirects)
		{
			var titles = new TitleCollection(this.Site);
			titles.AddFileUsage(new[] { this });

			return titles;
		}

		/// <summary>Finds any files on the wiki that are duplicates of this one.</summary>
		/// <returns>A list of duplicate file titles.</returns>
		public TitleCollection FindDuplicateFiles() => this.FindDuplicateFiles(true);

		/// <summary>Finds any files on the wiki that are duplicates of this one.</summary>
		/// <param name="localOnly">if set to <c>true</c>, only searches on the local wiki, ignoring shared repositories like Wikimedia Commons.</param>
		/// <returns>A collection of duplicate file titles.</returns>
		public TitleCollection FindDuplicateFiles(bool localOnly)
		{
			var titles = new TitleCollection(this.Site);
			titles.AddDuplicateFiles(new[] { this });

			return titles;
		}
		#endregion

		#region Protected Override Methods

		/// <summary>Populates file properties with relevant data from the WallE PageItem.</summary>
		/// <param name="pageItem">The page item.</param>
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
						size: imageInfoEntry.Size,
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