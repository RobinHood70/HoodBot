namespace RobinHood70.WikiCommon.RequestBuilder
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using static RobinHood70.CommonCode.Globals;
	/// <summary>Formats a Request object for use in a URL or POST data.</summary>
	public class RequestVisitorUrl : IParameterVisitor
	{
		#region Fields
		private readonly StringBuilder builder;
		private readonly bool supportsUnitSeparator;
		#endregion

		#region Constructors
		private RequestVisitorUrl(StringBuilder builder, bool supportsUnitSeparator)
		{
			this.builder = builder;
			this.supportsUnitSeparator = supportsUnitSeparator;
		}
		#endregion

		#region Public Static Methods

		/// <summary>Builds the specified request.</summary>
		/// <param name="request">The request.</param>
		/// <returns>A string representing the parameters, as they would be used in a URL or POST data.</returns>
		public static string Build(Request request)
		{
			ThrowNull(request, nameof(request));
			var builder = new StringBuilder();
			var visitor = new RequestVisitorUrl(builder, request.SupportsUnitSeparator);
			request.Build(visitor);
			return builder.ToString();
		}
		#endregion

		#region IParameterVisitor Methods

		/// <summary>Visits the specified FileParameter object.</summary>
		/// <param name="parameter">The FileParameter object.</param>
		/// <exception cref="NotSupportedException">Because a FileParameter is invalid for submission in a URL or POST data, this method always throws a NotSupportedException.</exception>
		public void Visit(FileParameter parameter) => throw new NotSupportedException();

		/// <summary>Visits the specified FormatParameter object.</summary>
		/// <param name="parameter">The FormatParameter object.</param>
		public void Visit(FormatParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			this.BuildParameterName(parameter);
			this.builder.Append(parameter.Value);
		}

		/// <summary>Visits the specified HiddenParameter object.</summary>
		/// <param name="parameter">The HiddenParameter object.</param>
		public void Visit(HiddenParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			this.BuildParameterName(parameter);
			this.builder.Append(EscapeDataString(parameter.Value));
		}

		/// <summary>Visits the specified PipedParameter or PipedListParameter object.</summary>
		/// <typeparam name="T">An enumerable string collection.</typeparam>
		/// <param name="parameter">The PipedParameter or PipedListParameter object.</param>
		/// <remarks>In all cases, the PipedParameter and PipedListParameter objects are treated identically, however the value collections they're associated with differ, so the Visit method is made generic to handle both.</remarks>
		public void Visit<T>(Parameter<T> parameter)
			where T : IEnumerable<string>
		{
			ThrowNull(parameter, nameof(parameter));
			this.BuildParameterName(parameter);
			var value = parameter.BuildPipedValue(this.supportsUnitSeparator);
			this.builder.Append(EscapeDataString(value));
		}

		/// <summary>Visits the specified StringParameter object.</summary>
		/// <param name="parameter">The StringParameter object.</param>
		public void Visit(StringParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			this.BuildParameterName(parameter);
			this.builder.Append(EscapeDataString(parameter.Value ?? string.Empty));
		}
		#endregion

		#region Private Methods
		private void BuildParameterName(IParameter parameter)
		{
			if (this.builder.Length > 0)
			{
				this.builder.Append('&');
			}

			this.builder
				.Append(parameter.Name)
				.Append('=');
		}
		#endregion
	}
}