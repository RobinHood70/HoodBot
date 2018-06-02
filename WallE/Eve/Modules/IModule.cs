namespace RobinHood70.WallE.Eve.Modules
{
	/// <summary>Specifies the set of properties required by all module implementations.</summary>
	public interface IModule
	{
		#region Properties

		/// <summary>Gets the name of the continue parameter for the module.</summary>
		/// <value>The name of the continue parameter for the module.</value>
		string ContinueName { get; }

		/// <summary>Gets the minimum MediaWiki version the module is available in.</summary>
		/// <value>The minimum MediaWiki version, expressed as an integer (i.e., MW 1.23 = 123).</value>
		int MinimumVersion { get; }

		/// <summary>Gets the module's name, as specified in the MediaWiki API.</summary>
		/// <value>The module's name.</value>
		string Name { get; }

		/// <summary>Gets the module's prefix, as specified in the MediaWiki API.</summary>
		/// <value>The module's prefix.</value>
		string FullPrefix { get; }
		#endregion
	}
}
