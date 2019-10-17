#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	[Flags]
	public enum ParameterFlags
	{
		None = 0,
		AllowsDuplicates = 1,
		Deprecated = 1 << 1,
		EnforceRange = 1 << 2,
		Multivalued = 1 << 3,
		Required = 1 << 4
	}

	public class ParametersItem
	{
		#region Public Properties
		public object? Default { get; set; }

		public RawMessageInfo Description { get; set; }

		public RawMessageInfo DynamicParameters { get; set; }

		public ParameterFlags Flags { get; set; }

		public int HighLimit { get; set; }

		public int HighMaximum { get; set; }

		public IReadOnlyList<InformationItem> Information { get; set; }

		public int Limit { get; set; }

		public int LowLimit { get; set; }

		public int Maximum { get; set; }

		public int Minimum { get; set; }

		public IReadOnlyList<ParametersItem> Parameters { get; set; }

		public IReadOnlyDictionary<string, string> Submodules { get; set; }

		public string? SubmoduleParameterPrefix { get; set; }

		public string? TokenType { get; set; }

		public string? Type { get; set; }

		public IReadOnlyList<string> TypeValues { get; set; }
		#endregion
	}
}
