#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

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
	#region Constructors
	internal ParametersItem(string name, object? dflt, RawMessageInfo description, ParameterFlags flags, int highLimit, int highMaximum, List<InformationItem> information, int limit, int lowLimit, int maximum, int minimum, string? subModuleParameterPrefix, IReadOnlyDictionary<string, string> subModules, string? tokenType, string? type, IReadOnlyList<string> typeValues)
	{
		this.Name = name;
		this.Default = dflt;
		this.Description = description;
		this.Flags = flags;
		this.HighLimit = highLimit;
		this.HighMaximum = highMaximum;
		this.Information = information;
		this.Limit = limit;
		this.LowLimit = lowLimit;
		this.Maximum = maximum;
		this.Minimum = minimum;
		this.SubmoduleParameterPrefix = subModuleParameterPrefix;
		this.Submodules = subModules;
		this.TokenType = tokenType;
		this.Type = type;
		this.TypeValues = typeValues;
	}
	#endregion

	#region Public Properties
	public object? Default { get; }

	public RawMessageInfo Description { get; }

	public ParameterFlags Flags { get; }

	public int HighLimit { get; }

	public int HighMaximum { get; }

	public IReadOnlyList<InformationItem> Information { get; }

	public int Limit { get; }

	public int LowLimit { get; }

	public int Maximum { get; }

	public int Minimum { get; }

	public string Name { get; }

	public IReadOnlyDictionary<string, string> Submodules { get; }

	public string? SubmoduleParameterPrefix { get; }

	public string? TokenType { get; }

	public string? Type { get; }

	public IReadOnlyList<string> TypeValues { get; }
	#endregion
}