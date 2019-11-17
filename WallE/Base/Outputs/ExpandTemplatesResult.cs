#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.WikiCommon;

	public class ExpandTemplatesResult
	{
		#region Constructors
		internal ExpandTemplatesResult(IReadOnlyList<string> categories, IReadOnlyDictionary<string, string> javaScriptConfigVars, IReadOnlyList<string> moduleScripts, IReadOnlyList<string> moduleStyles, IReadOnlyList<string> modules, string? parseTree, IReadOnlyDictionary<string, string?> properties, TimeSpan timeToLive, bool vol, string? wikiText)
		{
			this.Categories = categories;
			this.JavaScriptConfigVariables = javaScriptConfigVars;
			this.ModuleScripts = moduleScripts;
			this.ModuleStyles = moduleStyles;
			this.Modules = modules;
			this.ParseTree = parseTree;
			this.Properties = properties;
			this.TimeToLive = timeToLive;
			this.Volatile = vol;
			this.WikiText = wikiText;
		}
		#endregion

		#region Public Properties
		public IReadOnlyList<string> Categories { get; }

		public IReadOnlyDictionary<string, string> JavaScriptConfigVariables { get; }

		public IReadOnlyList<string> Modules { get; }

		public IReadOnlyList<string> ModuleScripts { get; }

		public IReadOnlyList<string> ModuleStyles { get; }

		public string? ParseTree { get; }

		public IReadOnlyDictionary<string, string?> Properties { get; }

		public TimeSpan TimeToLive { get; }

		public bool Volatile { get; }

		public string? WikiText { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.WikiText?.Ellipsis(30) ?? this.ParseTree?.Ellipsis(30) ?? this.GetType().Name;
		#endregion
	}
}
