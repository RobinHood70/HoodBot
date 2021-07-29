#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;
	using RobinHood70.CommonCode;

	public enum HelpFormat
	{
		None,
		Html,
		Raw,
		WikiText
	}

	public class ParameterInfoInput
	{
		#region Public Properties
		public ParameterInfoInput(IEnumerable<string> modules) => this.Modules = modules.NotNullOrWhiteSpace(nameof(modules));

		/// <summary>Gets or sets the modules to retrieve parameter information for.</summary>
		/// <value>The modules.</value>
		/// <remarks>Use "module+submodule" to get sub-module parameters. On older wikis, this will be converted internally as needed.</remarks>
		public IEnumerable<string> Modules { get; set; }

		public HelpFormat HelpFormat { get; set; }
		#endregion
	}
}
