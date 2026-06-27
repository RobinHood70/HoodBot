namespace RobinHood70.Robby;

using System;
using System.Collections.Generic;
using RobinHood70.WallE.Base;
using RobinHood70.WallE.Eve.Modules;

/// <summary>Represents a file on the wiki. Includes all page data as well as file revisions and file-specific methods.</summary>
/// <seealso cref="Page" />
public sealed class FilePageModule
{
	#region Public Constants

	/// <summary>Gets the property name for the file revisions module.</summary>
	public const string PropertyName = PropImageInfo.ModuleName;
	#endregion

	#region Fields
	private readonly List<FileRevision> fileRevisions = [];
	#endregion

	#region Constructors

	/// <summary>Initializes a new instance of the <see cref="FilePageModule"/> class.</summary>
	/// <param name="imageInfos">The API item to extract information from.</param>
	public FilePageModule(IEnumerable<ImageInfoResult> imageInfos)
	{
		ArgumentNullException.ThrowIfNull(imageInfos);
		this.fileRevisions.Clear();
		var latest = DateTime.MinValue;
		foreach (var imageInfo in imageInfos)
		{
			foreach (var imageInfoEntry in imageInfo)
			{
				FileRevision fileRevision = new(
					bitDepth: imageInfoEntry.BitDepth,
					size: imageInfoEntry.Size,
					height: imageInfoEntry.Height,
					width: imageInfoEntry.Width,
					comment: imageInfoEntry.Comment,
					mimeType: imageInfoEntry.MimeType,
					sha1: imageInfoEntry.Sha1,
					user: imageInfoEntry.User,
					timestamp: imageInfoEntry.Timestamp,
					uri: imageInfoEntry.Uri == null ? null : new Uri(imageInfoEntry.Uri));
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

	#region Public Properties

	/// <summary>Gets the list of file revisions.</summary>
	/// <value>The file revisions.</value>
	public IReadOnlyList<FileRevision> FileRevisions => this.fileRevisions;

	/// <summary>Gets the latest file revision.</summary>
	/// <value>The latest file revision.</value>
	public FileRevision? LatestFileRevision { get; }
	#endregion

	#region Public Static Methods

	/// <summary>Parses the result of a category information query and returns a <see cref="CategoriesPageModule" /> instance.</summary>
	/// <param name="result">The result to parse.</param>
	/// <exception cref="InvalidOperationException">Thrown when the result is not of the expected type.</exception>
	public static (string Key, object Value) ParseImageInfoResult(object result)
	{
		ArgumentNullException.ThrowIfNull(result);
		return result is List<ImageInfoResult> imageInfo
			? (PropertyName, new FilePageModule(imageInfo))
			: throw new InvalidOperationException($"Unexpected result type: {result.GetType().FullName}");
	}
	#endregion
}