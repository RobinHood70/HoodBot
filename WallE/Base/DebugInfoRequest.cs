#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System;
using System.Collections.Generic;

public class DebugInfoRequest(IReadOnlyDictionary<string, string> headers, string method, IReadOnlyDictionary<string, string> parameters, Uri url)
{
	#region Public Properties
	public IReadOnlyDictionary<string, string> Headers { get; } = headers;

	public string Method { get; } = method;

	public IReadOnlyDictionary<string, string> Parameters { get; } = parameters;

	public Uri Url { get; } = url;
	#endregion

	#region Public Override Methods
	public override string ToString() => $"({this.Method}) {this.Url}";
	#endregion
}