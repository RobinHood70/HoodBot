namespace RobinHood70.WallE.RequestBuilder
{
	using System;
	using System.Collections.Generic;
	using System.Net.Http;
	using static RobinHood70.WallE.ProjectGlobals;
	using static RobinHood70.WikiCommon.Globals;

	internal class RequestVisitorHttpContentUrl : IParameterVisitor
	{
		#region Fields
		private Dictionary<string, string> parameters;
		private bool supportsUnitSeparator;
		#endregion

		#region Constructors
		private RequestVisitorHttpContentUrl()
		{
		}
		#endregion

		#region Public Methods
		public static FormUrlEncodedContent Build(Request request)
		{
			var visitor = new RequestVisitorHttpContentUrl
			{
				supportsUnitSeparator = request.SupportsUnitSeparator,
				parameters = new Dictionary<string, string>(),
			};

			foreach (var parameter in request)
			{
				parameter.Accept(visitor);
			}

			return new FormUrlEncodedContent(visitor.parameters);
		}
		#endregion

		#region IParameterVisitor Methods
		public void Visit(FileParameter parameter) => throw new NotSupportedException();

		public void Visit(FormatParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			this.parameters.Add(parameter.Name, parameter.Value);
		}

		public void Visit(HiddenParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			this.parameters.Add(parameter.Name, parameter.Value);
		}

		public void Visit<T>(Parameter<T> parameter)
			where T : IEnumerable<string>
		{
			var value = BuildPipedValue(parameter, this.supportsUnitSeparator);
			this.parameters.Add(parameter.Name, value);
		}

		public void Visit(StringParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			this.parameters.Add(parameter.Name, parameter.Value);
		}
		#endregion
	}
}
