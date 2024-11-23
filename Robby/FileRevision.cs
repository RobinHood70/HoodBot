﻿namespace RobinHood70.Robby;

using System;

/// <summary>Stores all information related to a file revision.</summary>
public class FileRevision
{
	/// <summary>Initializes a new instance of the <see cref="FileRevision"/> class.</summary>
	/// <param name="bitDepth">If the file is an image, the bit depth of the image.</param>
	/// <param name="size">The size of the file.</param>
	/// <param name="height">If the file is an image, the height of the image.</param>
	/// <param name="width">If the file is an image, the width of the image.</param>
	/// <param name="comment">The file revision comment.</param>
	/// <param name="mimeType">The MIME type of the file.</param>
	/// <param name="sha1">The Sha1 checksum for the file.</param>
	/// <param name="user">The user who uploaded the file.</param>
	/// <param name="timestamp">The timestamp on the upload.</param>
	/// <param name="uri">The URI to the specific version of the file.</param>
	protected internal FileRevision(int bitDepth, int size, int height, int width, string? comment, string? mimeType, string? sha1, string? user, DateTime? timestamp, Uri? uri)
	{
		this.BitDepth = bitDepth;
		this.Comment = comment;
		this.Size = size;
		this.Uri = uri;
		this.Height = height;
		this.MimeType = mimeType;
		this.Sha1 = sha1;
		this.Timestamp = timestamp;
		this.User = user;
		this.Width = width;
	}

	/// <summary>Gets the bit depth of the image.</summary>
	/// <value>The bit depth of the image.</value>
	public int BitDepth { get; }

	/// <summary>Gets the file revision comment.</summary>
	/// <value>The file revision comment.</value>
	public string? Comment { get; }

	/// <summary>Gets the height of the image.</summary>
	/// <value>The height of the image.</value>
	public int Height { get; }

	/// <summary>Gets the MIME type of the file.</summary>
	/// <value>The MIME type of the file.</value>
	public string? MimeType { get; }

	/// <summary>Gets the Sha1 checksum for the file.</summary>
	/// <value>The Sha1 checksum.</value>
	public string? Sha1 { get; }

	/// <summary>Gets the size of the file.</summary>
	/// <value>The size of the file.</value>
	public int Size { get; }

	/// <summary>Gets the timestamp when the file was uploaded.</summary>
	/// <value>The timestamp.</value>
	public DateTime? Timestamp { get; }

	/// <summary>Gets the URI to the specific version of the file.</summary>
	/// <value>The URI.</value>
	public Uri? Uri { get; }

	/// <summary>Gets the user who uploaded the file.</summary>
	/// <value>The user who uploaded the file.</value>
	public string? User { get; }

	/// <summary>Gets the width of the image.</summary>
	/// <value>The width of the image.</value>
	public int Width { get; }
}