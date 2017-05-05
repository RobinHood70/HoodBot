namespace RobinHood70.WallE.RequestBuilder
{
	using System.Collections.Generic;
	using System.Net.Http;
	using static ProjectGlobals;
	using static WikiCommon.Globals;

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Disposed of by caller.")]
	internal class RequestVisitorHttpContentMultipart : IParameterVisitor
	{
		#region Fields
		private MultipartFormDataContent multipartData;
		private bool supportsUnitSeparator;
		#endregion

		#region Constructors
		private RequestVisitorHttpContentMultipart()
		{
		}
		#endregion

		#region Public Methods
		public static MultipartFormDataContent Build(Request request)
		{
			var visitor = new RequestVisitorHttpContentMultipart()
			{
				multipartData = new MultipartFormDataContent(),
				supportsUnitSeparator = request.SupportsUnitSeparator,
			};

			foreach (var param in request)
			{
				param.Accept(visitor);
			}

			return visitor.multipartData;
		}
		#endregion

		#region IParameterVisitor Methods
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed of by caller.")]
		public void Visit(FileParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			this.multipartData.Add(new ByteArrayContent(parameter.Value), parameter.Name, parameter.FileName);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed of by caller.")]
		public void Visit(FormatParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			this.multipartData.Add(new StringContent(parameter.Value), parameter.Name);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed of by caller.")]
		public void Visit(HiddenParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			this.multipartData.Add(new StringContent(parameter.Value), parameter.Name);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed of by caller.")]
		public void Visit<T>(Parameter<T> parameter)
			where T : IEnumerable<string>
		{
			var value = BuildPipedValue(parameter, this.supportsUnitSeparator);
			this.multipartData.Add(new StringContent(value), parameter.Name);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed of by caller.")]
		public void Visit(StringParameter parameter)
		{
			ThrowNull(parameter, nameof(parameter));
			this.multipartData.Add(new StringContent(parameter.Value), parameter.Name);
		}
		#endregion
	}
}
