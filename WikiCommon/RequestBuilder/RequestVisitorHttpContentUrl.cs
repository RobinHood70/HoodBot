namespace RobinHood70.WikiCommon.RequestBuilder
{
	using System;
	using System.Collections.Generic;
	using System.Net.Http;
	using static RobinHood70.CommonCode.Globals;

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
			ThrowNull(request, nameof(request));
			var visitor = new RequestVisitorHttpContentUrl
			{
				supportsUnitSeparator = request.SupportsUnitSeparator,
			};

			visitor.parameters.Clear();
			request.Build(visitor);

#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types. FormUrlEncodedContent only supports KVP<string?, string?>, while Dictionary explicitly forbids null in the key value.
			return new FormUrlEncodedContent(visitor.parameters);
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
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
			ThrowNull(parameter, nameof(parameter));
			var value = parameter.BuildPipedValue(this.supportsUnitSeparator);
			this.parameters.Add(parameter.Name, value);
		}

		/// <summary>Visits the specified StringParameter object.</summary>
		/// <param name="parameter">The StringParameter object.</param>
		public void Visit(StringParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			this.parameters.Add(parameter.Name, parameter.Value);
		}
		#endregion
	}
}
