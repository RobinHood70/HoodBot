﻿#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	[Flags]
	public enum ModuleFlags
	{
		None = 0,
		Deprecated = 1,
		Generator = 1 << 1,
		Internal = 1 << 2,
		MustBePosted = 1 << 3,
		ReadRights = 1 << 4,
		WriteRights = 1 << 5
	}

	// IMPNOTE: Older "version", "props" and "errors" return values are currently not implemented, though they would be easy enough to implement if anyone has need of them.
	public class ParameterInfoItem
	{
		#region Public Properties
		public string ClassName { get; set; }

		public RawMessageInfo Description { get; set; }

		public RawMessageInfo DynamicParameters { get; set; }

		public IReadOnlyList<ExamplesItem> Examples { get; set; }

		public string Group { get; set; }

		public IReadOnlyList<string> HelpUrls { get; set; }

		public string LicenseLink { get; set; }

		public string LicenseTag { get; set; }

		public ModuleFlags Flags { get; set; }

		public string Name { get; set; }

		public IReadOnlyDictionary<string, ParametersItem> Parameters { get; set; }

		public string Path { get; set; }

		public string Prefix { get; set; }

		public string Source { get; set; }

		public string SourceName { get; set; }
		#endregion
	}
}
