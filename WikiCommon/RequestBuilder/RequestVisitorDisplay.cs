namespace RobinHood70.WikiCommon.RequestBuilder
{
	using System;
	using System.Text;
	using static RobinHood70.CommonCode.Globals;

	// Escaping in this class is only at the Uri level rather than the Data level because it produces much cleaner output which any browser will fix up, if needed, when the request is put through.

	/// <summary>Formats a Request object for display purposes, hiding parameters that should not be revealed.</summary>
	internal class RequestVisitorDisplay : IParameterVisitor
	{
		#region Fields
		private readonly StringBuilder builder;
		#endregion

		#region Constructors
		private RequestVisitorDisplay(StringBuilder builder) => this.builder = builder;
		#endregion

		#region IParameterVisitor Methods

		/// <summary>Builds the specified request.</summary>
		/// <param name="request">The request.</param>
		/// <returns>A string representing the parameters, formatted for display purposes.</returns>
		public static string Build(Request request)
		{
			var builder = new StringBuilder();
			var visitor = new RequestVisitorDisplay(builder);
			request.Build(visitor);
			builder.Replace("%20", "+");

			var methodText =
				request.Type == RequestType.Get ? "GET" :
				request.Type == RequestType.Post ? "POST" :
				"POST (multipart)";

			return Invariant($"{methodText}: {request.Uri}?{builder}");
		}

		public void Visit(FileParameter parameter)
		{
			this.BuildParameterName(parameter);
			this.builder.Append("<filedata>");
		}

		public void Visit(PipedParameter parameter)
		{
			this.BuildParameterName(parameter);
			var value = parameter.BuildPipedValue(false);
			this.builder.Append(EscapeDataString(value).Replace("%7C", "|", StringComparison.Ordinal).Replace("%20", "+", StringComparison.Ordinal));
		}

		public void Visit(StringParameter parameter)
		{
			this.BuildParameterName(parameter);
			this.builder.Append(parameter.ValueType switch
			{
				ValueType.Hidden => "<hidden>",
				ValueType.Modify => parameter.Value + "fm",
				_ => EscapeDataString(parameter?.Value ?? string.Empty),
			});
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
