namespace RobinHood70.WikiCommon.RequestBuilder
{
	using System.Net.Http;
	using static RobinHood70.CommonCode.Globals;

	/// <summary>Formats a Request object as <see cref="MultipartFormDataContent"/>.</summary>
	public class RequestVisitorHttpContentMultipart : IParameterVisitor
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

		#region Public Methods

		/// <summary>Builds the specified request.</summary>
		/// <param name="request">The request.</param>
		/// <returns>A <see cref="MultipartFormDataContent"/> object representing the parameters.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "This is the return value - caller is responsible for disposing.")]
		public static MultipartFormDataContent Build(Request request)
		{
			ThrowNull(request, nameof(request));
			var multipartData = new MultipartFormDataContent();
			var visitor = new RequestVisitorHttpContentMultipart(multipartData, request.SupportsUnitSeparator);
			request.Build(visitor);

			return multipartData;
		}
		#endregion

		#region IParameterVisitor Methods

		/// <summary>Visits the specified FileParameter object.</summary>
		/// <param name="parameter">The FileParameter object.</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "False positive.")]
		public void Visit(FileParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			this.multipartData.Add(new ByteArrayContent(parameter.GetFileData()), parameter.Name, parameter.FileName);
		}

		/// <summary>Visits the specified FormatParameter object.</summary>
		/// <param name="parameter">The FormatParameter object.</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "False positive.")]
		public void Visit(FormatParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			this.multipartData.Add(new StringContent(parameter.Value), parameter.Name);
		}

		/// <summary>Visits the specified HiddenParameter object.</summary>
		/// <param name="parameter">The HiddenParameter object.</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "False positive.")]
		public void Visit(HiddenParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			this.multipartData.Add(new StringContent(parameter.Value), parameter.Name);
		}

		/// <summary>Visits the specified PipedParameter or PipedListParameter object.</summary>
		/// <param name="parameter">The PipedParameter or PipedListParameter object.</param>
		/// <remarks>In all cases, the PipedParameter and PipedListParameter objects are treated identically, however the value collections they're associated with differ, so the Visit method is made generic to handle both.</remarks>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "False positive.")]
		public void Visit(MultiValuedParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			var value = parameter.BuildPipedValue(this.supportsUnitSeparator);
			this.multipartData.Add(new StringContent(value), parameter.Name);
		}

#pragma warning disable CA2000 // Dispose objects before losing scope

		/// <summary>Visits the specified StringParameter object.</summary>
		/// <param name="parameter">The StringParameter object.</param>
		public void Visit(StringParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));

			// StringContent wants to be disposed, but can't be at this stage. It's only relevant to async methods anyway. Container manages disposal in any event - a very bizarre "convenience feature" that a lot of people have complained about. This whole thing seems very strangely designed - might warrant using/creating something else at some point in the future.
			this.multipartData.Add(new StringContent(parameter.Value), parameter.Name);
		}
#pragma warning restore CA2000 // Dispose objects before losing scope
		#endregion
	}
}