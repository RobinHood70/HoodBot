namespace RobinHood70.WallE.RequestBuilder
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using static ProjectGlobals;
	using static WikiCommon.Globals;

	// Because this is an internal class, we take a few shortcuts in that not everything is UrlEncoded when we know that it never needs to be within the context of the project.
	internal class RequestVisitorUrl : IParameterVisitor
	{
		#region Fields
		private StringBuilder builder;
		private bool supportsUnitSeparator;
		#endregion

		#region Constructors
		private RequestVisitorUrl()
		{
		}
		#endregion

		#region IParameterVisitor Methods
		public static string Build(Request request)
		{
			var visitor = new RequestVisitorUrl();
			var builder = new StringBuilder();
			visitor.supportsUnitSeparator = request.SupportsUnitSeparator;
			visitor.builder = builder;
			foreach (var parameter in request)
			{
				builder
					.Append('&')
					.Append(parameter.Name)
					.Append('=');
				parameter.Accept(visitor);
			}

			if (builder.Length > 0)
			{
				builder.Remove(0, 1);
			}

			return builder.ToString().Replace("%20", "+");
		}

		public void Visit(FileParameter parameter) => throw new NotSupportedException();

		public void Visit(FormatParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			this.builder.Append(parameter.Value);
		}

		public void Visit(HiddenParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			this.builder.Append(Uri.EscapeDataString(parameter.Value));
		}

		public void Visit<T>(Parameter<T> parameter)
			where T : IEnumerable<string>
		{
			ThrowNull(parameter, nameof(parameter));
			var value = BuildPipedValue(parameter, this.supportsUnitSeparator);
			this.builder.Append(Uri.EscapeDataString(value));
		}

		public void Visit(StringParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			this.builder.Append(Uri.EscapeDataString(parameter.Value ?? string.Empty));
		}
		#endregion
	}
}
