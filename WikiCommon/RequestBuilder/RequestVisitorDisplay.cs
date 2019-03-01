namespace RobinHood70.WikiCommon.RequestBuilder
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using static RobinHood70.WikiCommon.Globals;

	// Escaping in this class is only at the Uri level rather than the Data level because it produces much cleaner output which any browser will fix up, if needed, when the request is put through.

	/// <summary>Formats a Request object for display purposes, hiding parameters that should not be revealed.</summary>
	internal class RequestVisitorDisplay : IParameterVisitor
	{
		#region Fields
		private StringBuilder builder;
		#endregion

		#region Constructors
		private RequestVisitorDisplay()
		{
		}
		#endregion

		#region IParameterVisitor Methods

		/// <summary>Builds the specified request.</summary>
		/// <param name="parameters">A <see cref="Parameter{T}"/> to be formatted.</param>
		/// <returns>A string representing the parameters, formatted for display purposes.</returns>
		public static string BuildParameters(ParameterCollection parameters)
		{
			var sb = new StringBuilder();
			var visitor = new RequestVisitorDisplay() { builder = sb };
			foreach (var parameter in parameters)
			{
				sb
					.Append('&')
					.Append(parameter.Name)
					.Append('=');
				parameter.Accept(visitor);
			}

			if (sb.Length > 0)
			{
				sb.Remove(0, 1);
			}

			var query = sb.ToString();

			return query.Replace("%20", "+");
		}

		/// <summary>Builds the specified request.</summary>
		/// <param name="request">The request.</param>
		/// <returns>A string representing the parameters, formatted for display purposes.</returns>
		public static string Build(Request request)
		{
			var query = BuildParameters(request);
			var methodText =
				request.Type == RequestType.Get ? "GET" :
				request.Type == RequestType.Post ? "POST" :
				"POST (multipart)";

			return Invariant($"{methodText}: {request.Uri}?{query}");
		}

		public void Visit(FileParameter parameter) => this.builder.Append("<filedata>");

		public void Visit(FormatParameter parameter) => this.builder.Append(parameter?.Value).Append("fm");

		public void Visit(HiddenParameter parameter) => this.builder.Append("<hidden>");

		public void Visit<T>(Parameter<T> parameter)
			where T : IEnumerable<string>
		{
			var value = parameter.BuildPipedValue(false);
			this.builder.Append(Uri.EscapeDataString(value).Replace("%7C", "|"));
		}

		public void Visit(StringParameter parameter) => this.builder.Append(Uri.EscapeDataString(parameter?.Value ?? string.Empty));
		#endregion
	}
}
