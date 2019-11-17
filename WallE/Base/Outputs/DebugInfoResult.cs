#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	/// <summary>Stores debug info, if emitted by the API.</summary>
	public class DebugInfoResult
	{
		#region Constructors
		public DebugInfoResult(IReadOnlyList<string> debugLog, double elapsedTime, string? gitBranch, string? gitRevision, Uri? gitViewUrl, IReadOnlyList<DebugInfoInclude> includes, IReadOnlyList<DebugInfoLog> log, string mwVersion, string phpEngine, string phpVersion, IReadOnlyList<DebugInfoQuery> queries, DebugInfoRequest request)
		{
			this.DebugLog = debugLog;
			this.ElapsedTime = elapsedTime;
			this.GitBranch = gitBranch;
			this.GitRevision = gitRevision;
			this.GitViewUrl = gitViewUrl;
			this.Includes = includes;
			this.Log = log;
			this.MediaWikiVersion = mwVersion;
			this.PhpEngine = phpEngine;
			this.PhpVersion = phpVersion;
			this.Queries = queries;
			this.Request = request;
		}
		#endregion

		#region Public Properties
		public IReadOnlyList<string> DebugLog { get; }

		public double ElapsedTime { get; }

		public string? GitBranch { get; }

		public string? GitRevision { get; }

		public Uri? GitViewUrl { get; }

		public IReadOnlyList<DebugInfoInclude> Includes { get; }

		public IReadOnlyList<DebugInfoLog> Log { get; }

		public string MediaWikiVersion { get; }

		public string PhpEngine { get; }

		public string PhpVersion { get; }

		public IReadOnlyList<DebugInfoQuery> Queries { get; }

		public DebugInfoRequest Request { get; }
		#endregion
	}
}