namespace RobinHood70.WikiCommon.RequestBuilder
{
	using System;
	using System.Collections.Generic;
	using System.Net.Http;
	using RobinHood70.CommonCode;

	/// <summary>Formats a Request object as <see cref="FormUrlEncodedContent"/>.</summary>
	public sealed class RequestVisitorHttpContentUrl : IParameterVisitor
	{
		#region Fields
		private readonly Dictionary<string, string?> parameters = new(StringComparer.Ordinal);
		private bool supportsUnitSeparator;
		#endregion

		#region Constructors
		private RequestVisitorHttpContentUrl()
		{
		}
		#endregion

		#region Public Methods

		/// <summary>Builds the specified request.</summary>
		/// <param name="request">The request.</param>
		/// <returns>A string representing the parameters, as they would be used in a URL or POST data.</returns>
		public static FormUrlEncodedContent Build(Request request)
		{
			request.ThrowNull();
			RequestVisitorHttpContentUrl visitor = new()
			{
				supportsUnitSeparator = request.SupportsUnitSeparator,
			};

			request.Build(visitor);
			return new FormUrlEncodedContent(visitor.parameters);
		}
		#endregion

		#region IParameterVisitor Methods

		/// <summary>Visits the specified FileParameter object.</summary>
		/// <param name="parameter">The FileParameter object.</param>
		/// <exception cref="NotSupportedException">Because a FileParameter is invalid for submission in a URL or POST data, this method always throws a NotSupportedException.</exception>
		public void Visit(FileParameter parameter) => throw new NotSupportedException();

		/// <summary>Visits the specified PipedParameter or PipedListParameter object.</summary>
		/// <param name="parameter">The PipedParameter or PipedListParameter object.</param>
		/// <remarks>In all cases, the PipedParameter and PipedListParameter objects are treated identically, however the value collections they're associated with differ, so the Visit method is made generic to handle both.</remarks>
		public void Visit(PipedParameter parameter)
		{
			var value = parameter.NotNull().BuildPipedValue(this.supportsUnitSeparator);
			this.parameters.Add(parameter.Name, value);
		}

		/// <summary>Visits the specified StringParameter object.</summary>
		/// <param name="parameter">The StringParameter object.</param>
		public void Visit(StringParameter parameter) => this.parameters.Add(parameter.NotNull().Name, parameter.Value);
		#endregion
	}
}