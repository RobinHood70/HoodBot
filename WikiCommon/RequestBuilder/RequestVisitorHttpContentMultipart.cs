namespace RobinHood70.WikiCommon.RequestBuilder;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;

/// <summary>Formats a Request object as <see cref="MultipartContent"/>.</summary>
public sealed class RequestVisitorHttpContentMultipart : IParameterVisitor
{
	//// This class works around scope warnings by essentially recreating MultipartFormDataContent internally. Adding to the internal list then copying that list to the return value in Build() avoids said warnings when adding new ByteArrayContent or StringContent to an IDisposable. I'm not sure why this works, though, since adding new content objects directly in the Build loop instead of from the List causes the same warnings. One way or the other, all the content objects are disposed of by the MultipartContent object, so it shouldn't be a concern.

	#region Constants
	private const string FormData = "form-data";
	#endregion

	#region Fields
	private readonly List<HttpContent> multipartData = [];
	private readonly bool supportsUnitSeparator;
	#endregion

	#region Constructors
	private RequestVisitorHttpContentMultipart(bool supportsUnitSeparator)
	{
		this.supportsUnitSeparator = supportsUnitSeparator;
	}
	#endregion

	#region Public Static Methods

	/// <summary>Builds the specified request.</summary>
	/// <param name="request">The request.</param>
	/// <returns>A <see cref="MultipartContent"/> object representing the parameters.</returns>
	public static MultipartContent Build(Request request)
	{
		ArgumentNullException.ThrowIfNull(request);
		var visitor = new RequestVisitorHttpContentMultipart(request.SupportsUnitSeparator);
		request.Build(visitor);

		var retval = new MultipartContent(FormData);
		foreach (var param in visitor.multipartData)
		{
			retval.Add(param);
		}

		return retval;
	}
	#endregion

	#region IParameterVisitor Methods

	/// <summary>Visits the specified FileParameter object.</summary>
	/// <param name="parameter">The FileParameter object.</param>
	public void Visit(FileParameter parameter)
	{
		ArgumentNullException.ThrowIfNull(parameter);
		var content = new ByteArrayContent(parameter.GetData());
		content.Headers.ContentDisposition = new ContentDispositionHeaderValue(FormData)
		{
			Name = parameter.Name,
			FileName = parameter.FileName,
			FileNameStar = parameter.FileName
		};

		this.multipartData.Add(content);
	}

	/// <summary>Visits the specified PipedParameter or PipedListParameter object.</summary>
	/// <param name="parameter">The PipedParameter or PipedListParameter object.</param>
	/// <remarks>In all cases, the PipedParameter and PipedListParameter objects are treated identically, however the value collections they're associated with differ, so the Visit method is made generic to handle both.</remarks>
	public void Visit(PipedParameter parameter)
	{
		ArgumentNullException.ThrowIfNull(parameter);
		var value = parameter.BuildPipedValue(this.supportsUnitSeparator);
		var content = new StringContent(value);
		content.Headers.ContentDisposition = new ContentDispositionHeaderValue(FormData)
		{
			Name = parameter.Name
		};

		this.multipartData.Add(content);
	}

	/// <summary>Visits the specified StringParameter object.</summary>
	/// <param name="parameter">The StringParameter object.</param>
	public void Visit(StringParameter parameter)
	{
		ArgumentNullException.ThrowIfNull(parameter);
		var content = new StringContent(parameter.Value);
		content.Headers.ContentDisposition = new ContentDispositionHeaderValue(FormData)
		{
			Name = parameter.Name
		};

		this.multipartData.Add(content);
	}
	#endregion
}