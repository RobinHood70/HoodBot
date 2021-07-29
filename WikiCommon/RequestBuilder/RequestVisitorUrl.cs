namespace RobinHood70.WikiCommon.RequestBuilder
{
	using System;
	using System.Text;
	using RobinHood70.CommonCode;

	/// <summary>Formats a Request object for use in a URL or POST data.</summary>
	public sealed class RequestVisitorUrl : IParameterVisitor
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
			var sb = new StringBuilder();
			var visitor = new RequestVisitorUrl(sb, request.NotNull(nameof(request)).SupportsUnitSeparator);
			request.Build(visitor);
			return sb.ToString();
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
			this.BuildParameterName(parameter.NotNull(nameof(parameter)));
			var value = parameter.BuildPipedValue(this.supportsUnitSeparator);
			this.builder.Append(Globals.EscapeDataString(value));
		}

		/// <summary>Visits the specified StringParameter object.</summary>
		/// <param name="parameter">The StringParameter object.</param>
		public void Visit(StringParameter parameter)
		{
			this.BuildParameterName(parameter.NotNull(nameof(parameter)));
			this.builder.Append(Globals.EscapeDataString(parameter.Value ?? string.Empty));
		}
		#endregion

		#region Private Methods
		private void BuildParameterName(Parameter parameter)
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