namespace RobinHood70.WallE.RequestBuilder
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using static ProjectGlobals;
	using static WikiCommon.Globals;

	// Escaping in this class is only at the Uri level rather than the Data level because it produces much cleaner output which any browser will fix up, if needed, when the request is put through.
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
		public static string Build(ParameterCollection parameters)
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

		public static string Build(Request request)
		{
			var query = Build(request as ParameterCollection);
			var methodText =
				request.Type == RequestType.Get ? "GET" :
				request.Type == RequestType.Post ? "POST" :
				"POST (multipart)";

			return Invariant((FormattableString)$"{methodText}: {request.Uri}?{query}");
		}

		public void Visit(FileParameter parameter) => this.builder.Append("<filedata>");

		public void Visit(FormatParameter parameter) => this.builder.Append(parameter?.Value).Append("fm");

		public void Visit(HiddenParameter parameter) => this.builder.Append("<hidden>");

		public void Visit<T>(Parameter<T> parameter)
			where T : IEnumerable<string>
		{
			var value = BuildPipedValue(parameter, false);
			this.builder.Append(Uri.EscapeDataString(value).Replace("%7C", "|"));
		}

		public void Visit(StringParameter parameter) => this.builder.Append(Uri.EscapeDataString(parameter?.Value ?? string.Empty));
		#endregion
	}
}
