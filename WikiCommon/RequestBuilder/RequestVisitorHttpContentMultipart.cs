namespace RobinHood70.WikiCommon.RequestBuilder
{
	using System;
	using System.Net.Http;

	/// <summary>Formats a Request object as <see cref="MultipartFormDataContent"/>.</summary>
	public sealed class RequestVisitorHttpContentMultipart : IParameterVisitor
	{
		#region Fields
		private readonly MultipartFormDataContent multipartData;
		private readonly bool supportsUnitSeparator;
		#endregion

		#region Constructors
		private RequestVisitorHttpContentMultipart(MultipartFormDataContent multipartData, bool supportsUnitSeparator)
		{
			this.multipartData = multipartData;
			this.supportsUnitSeparator = supportsUnitSeparator;
		}
		#endregion

		#region Public Static Methods

		/// <summary>Builds the specified request.</summary>
		/// <param name="request">The request.</param>
		/// <returns>A <see cref="MultipartFormDataContent"/> object representing the parameters.</returns>
		public static MultipartFormDataContent Build(Request request)
		{
			// Note: the returned data should be iterated over and each individual HttpContent should be disposed.
			ArgumentNullException.ThrowIfNull(request);
			MultipartFormDataContent data = [];
			RequestVisitorHttpContentMultipart visitor = new(data, request.SupportsUnitSeparator);
			request.Build(visitor);

			return data;
		}
		#endregion

		#region IParameterVisitor Methods

		// All HttpContent wants to be disposed, but can't be during each individual part of the build process. So instead, we dispose of all of it when this object is disposed of.
#pragma warning disable CA2000 // Dispose objects before losing scope: Disposed by multipart object itself.

		/// <summary>Visits the specified FileParameter object.</summary>
		/// <param name="parameter">The FileParameter object.</param>
		public void Visit(FileParameter parameter)
		{
			ArgumentNullException.ThrowIfNull(parameter);
			this.multipartData.Add(new ByteArrayContent(parameter.GetData()), parameter.Name, parameter.FileName);
		}

		/// <summary>Visits the specified PipedParameter or PipedListParameter object.</summary>
		/// <param name="parameter">The PipedParameter or PipedListParameter object.</param>
		/// <remarks>In all cases, the PipedParameter and PipedListParameter objects are treated identically, however the value collections they're associated with differ, so the Visit method is made generic to handle both.</remarks>
		public void Visit(PipedParameter parameter)
		{
			ArgumentNullException.ThrowIfNull(parameter);
			var value = parameter.BuildPipedValue(this.supportsUnitSeparator);
			this.multipartData.Add(new StringContent(value), parameter.Name);
		}

		/// <summary>Visits the specified StringParameter object.</summary>
		/// <param name="parameter">The StringParameter object.</param>
		public void Visit(StringParameter parameter)
		{
			ArgumentNullException.ThrowIfNull(parameter);
			this.multipartData.Add(new StringContent(parameter.Value), parameter.Name);
		}
#pragma warning restore CA2000
		#endregion
	}
}