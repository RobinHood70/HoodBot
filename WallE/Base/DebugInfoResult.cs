#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System;
using System.Collections.Generic;

/// <summary>Stores debug info, if emitted by the API.</summary>
public class DebugInfoResult(IReadOnlyList<string> debugLog, double elapsedTime, string? gitBranch, string? gitRevision, Uri? gitViewUrl, IReadOnlyList<DebugInfoInclude> includes, IReadOnlyList<DebugInfoLog> log, string mwVersion, string phpEngine, string phpVersion, IReadOnlyList<DebugInfoQuery> queries, DebugInfoRequest request)
{
	#region Public Properties
	public IReadOnlyList<string> DebugLog { get; } = debugLog;

	public double ElapsedTime { get; } = elapsedTime;

	public string? GitBranch { get; } = gitBranch;

	public string? GitRevision { get; } = gitRevision;

	public Uri? GitViewUrl { get; } = gitViewUrl;

	public IReadOnlyList<DebugInfoInclude> Includes { get; } = includes;

	public IReadOnlyList<DebugInfoLog> Log { get; } = log;

	public string MediaWikiVersion { get; } = mwVersion;

	public string PhpEngine { get; } = phpEngine;

	public string PhpVersion { get; } = phpVersion;

	public IReadOnlyList<DebugInfoQuery> Queries { get; } = queries;

	public DebugInfoRequest Request { get; } = request;
	#endregion
}