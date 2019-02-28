namespace RobinHood70.WallE.RequestBuilder
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using static RobinHood70.WallE.ProjectGlobals;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Formats a Request object for use in a URL or POST data.</summary>
	public class RequestVisitorUrl : IParameterVisitor
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

		/// <summary>Builds the specified request.</summary>
		/// <param name="request">The request.</param>
		/// <returns>A string representing the parameters, as they would be used in a URL or POST data.</returns>
		public static string Build(Request request)
		{
			ThrowNull(request, nameof(request));
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

		/// <summary>Visits the specified FileParameter object.</summary>
		/// <param name="parameter">The FileParameter object.</param>
		/// <exception cref="NotSupportedException">Because a FileParameter is invalid for submission in a URL or POST data, this method always throws a NotSupportedException.</exception>
		public void Visit(FileParameter parameter) => throw new NotSupportedException();

		/// <summary>Visits the specified FormatParameter object.</summary>
		/// <param name="parameter">The FormatParameter object.</param>
		public void Visit(FormatParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			this.builder.Append(parameter.Value);
		}

		/// <summary>Visits the specified HiddenParameter object.</summary>
		/// <param name="parameter">The HiddenParameter object.</param>
		public void Visit(HiddenParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			this.builder.Append(Uri.EscapeDataString(parameter.Value));
		}

		/// <summary>Visits the specified PipedParameter or PipedListParameter object.</summary>
		/// <typeparam name="T">An enumerable string collection.</typeparam>
		/// <param name="parameter">The PipedParameter or PipedListParameter object.</param>
		/// <remarks>In all cases, the PipedParameter and PipedListParameter objects are treated identically, however the value collections they're associated with differ, so the Visit method is made generic to handle both.</remarks>
		public void Visit<T>(Parameter<T> parameter)
			where T : IEnumerable<string>
		{
			ThrowNull(parameter, nameof(parameter));
			var value = BuildPipedValue(parameter, this.supportsUnitSeparator);
			this.builder.Append(Uri.EscapeDataString(value));
		}

		/// <summary>Visits the specified StringParameter object.</summary>
		/// <param name="parameter">The StringParameter object.</param>
		public void Visit(StringParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			this.builder.Append(Uri.EscapeDataString(parameter.Value ?? string.Empty));
		}
		#endregion
	}
}
