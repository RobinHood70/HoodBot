#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	public class ExpandTemplatesResult
	{
		#region Public Properties
		public IReadOnlyList<string> Categories { get; set; }

		public IReadOnlyDictionary<string, string> JavaScriptConfigVariables { get; set; }

		public IReadOnlyList<string> Modules { get; set; }

		public IReadOnlyList<string> ModuleScripts { get; set; }

		public IReadOnlyList<string> ModuleStyles { get; set; }

		public string ParseTree { get; set; }

		public IReadOnlyDictionary<string, string> Properties { get; set; }

		public TimeSpan TimeToLive { get; set; }

		public bool Volatile { get; set; }

		public string WikiText { get; set; }
		#endregion
	}
}
