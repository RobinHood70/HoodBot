#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System;
using System.Collections.Generic;

public class HelpInput
{
	#region Public Constructors
	public HelpInput(IEnumerable<string> modules)
	{
		ArgumentNullException.ThrowIfNull(modules);
		foreach (var module in modules)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(module);
		}

		this.Modules = modules;
	}
	#endregion

	#region Public Properties

	/// <summary>Gets the modules to retrieve help for.</summary>
	/// <value>The modules.</value>
	/// <remarks>Use "module+submodule" to get sub-module help. On older wikis, this will be converted internally as needed.</remarks>
	public IEnumerable<string> Modules { get; }

	public bool RecursiveSubModules { get; set; }

	public bool SubModules { get; set; }

	public bool Toc { get; set; }
	#endregion
}