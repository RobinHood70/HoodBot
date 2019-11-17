#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	public class DebugInfoRequest
	{
		#region Constructors
		public DebugInfoRequest(IReadOnlyDictionary<string, string> headers, string method, IReadOnlyDictionary<string, string> parameters, Uri url)
		{
			this.Headers = headers;
			this.Method = method;
			this.Parameters = parameters;
			this.Url = url;
		}
		#endregion

		#region Public Properties
		public IReadOnlyDictionary<string, string> Headers { get; }

		public string Method { get; }

		public IReadOnlyDictionary<string, string> Parameters { get; }

		public Uri Url { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => $"({this.Method}) {this.Url}";
		#endregion
	}
}
