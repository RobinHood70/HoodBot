namespace RobinHood70.WikiCommon.RequestBuilder;

/// <summary>A simple type for multipart data, which must return both the content type as well as the data to send.</summary>
/// <remarks>Initializes a new instance of the <see cref="MultipartResult" /> class.</remarks>
/// <param name="contentType">The value for the HTML <c>Content-Type</c> header.</param>
/// <param name="data">The byte data to send.</param>
public class MultipartResult(string contentType, byte[] data)
{
	#region Fields
	private readonly byte[] data = data;
	#endregion

	#region Public Properties

	/// <summary>Gets the value for the HTML <c>Content-Type</c> header.</summary>
	/// <value>The HTML content type.</value>
	public string ContentType { get; } = contentType;
	#endregion

	#region Public Methods

	/// <summary>Gets the byte data to send.</summary>
	/// <returns>The byte data to send.</returns>
	public byte[] GetData() => this.data;
	#endregion
}