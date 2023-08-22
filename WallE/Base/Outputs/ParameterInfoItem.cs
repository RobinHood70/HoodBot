#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
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

	// Added MW 1.32 TemplatedParameters since I was working with parameters anyway, but the rest of module is pre-1.32.
	// IMPNOTE: Older "version", "props" and "errors" return values are currently not implemented, though they would be easy enough to implement if anyone has need of them.
	public class ParameterInfoItem
	{
		internal ParameterInfoItem(string className, string path, string prefix, IReadOnlyList<string> helpUrls, IReadOnlyDictionary<string, ParametersItem> parameters, string? name, RawMessageInfo description, RawMessageInfo dynamicParameters, List<ExamplesItem> examples, ModuleFlags flags, string? group, string? licenseLink, string? licenseTag, string? source, string? sourceName, IReadOnlyDictionary<string, ParametersItem> templatedParameters)
		{
			this.ClassName = className;
			this.Path = path;
			this.Prefix = prefix;
			this.HelpUrls = helpUrls;
			this.Parameters = parameters;
			this.Name = name;
			this.Description = description;
			this.DynamicParameters = dynamicParameters;
			this.Examples = examples;
			this.Flags = flags;
			this.Group = group;
			this.LicenseLink = licenseLink;
			this.LicenseTag = licenseTag;
			this.Source = source;
			this.SourceName = sourceName;
			this.TemplatedParameters = templatedParameters;
		}

		#region Public Properties
		public string ClassName { get; }

		public RawMessageInfo Description { get; }

		public RawMessageInfo DynamicParameters { get; }

		public IReadOnlyList<ExamplesItem> Examples { get; }

		public ModuleFlags Flags { get; }

		public string? Group { get; }

		public IReadOnlyList<string> HelpUrls { get; }

		public string? LicenseLink { get; }

		public string? LicenseTag { get; }

		public string? Name { get; }

		public IReadOnlyDictionary<string, ParametersItem> Parameters { get; }

		public string Path { get; }

		public string Prefix { get; }

		public string? Source { get; }

		public string? SourceName { get; }

		public IReadOnlyDictionary<string, ParametersItem> TemplatedParameters { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.ClassName;
		#endregion
	}
}