﻿namespace RobinHood70.WikiCommon.RequestBuilder
{
	/// <summary>A simple type for multipart data, which must return both the content type as well as the data to send.</summary>
	public class MultipartResult
	{
		/// <summary>Initializes a new instance of the <see cref="MultipartResult" /> class.</summary>
		/// <param name="contentType">The value for the HTML <c>Content-Type</c> header.</param>
		/// <param name="data">The byte data to send.</param>
		public MultipartResult(string contentType, byte[] data)
		{
			this.ContentType = contentType;
			this.Data = data;
		}

		/// <summary>Gets the value for the HTML <c>Content-Type</c> header.</summary>
		/// <value>The HTML content type.</value>
		public string ContentType { get; }

		/// <summary>Gets the byte data to send.</summary>
		/// <value>The byte data to send.</value>
		public byte[] Data { get; }
	}
}
