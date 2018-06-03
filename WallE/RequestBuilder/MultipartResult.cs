namespace RobinHood70.WallE.RequestBuilder
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
		public string ContentType { get; }

		/// <summary>Gets the byte data to send.</summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Not intended for use by caller, just to be passed along.")]
		public byte[] Data { get; }
	}
}
