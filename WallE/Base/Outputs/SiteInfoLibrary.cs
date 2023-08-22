#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class SiteInfoLibrary
	{
		#region Constructors
		internal SiteInfoLibrary(string name, string version)
		{
			this.Name = name;
			this.Version = version;
		}
		#endregion

		#region Public Properties
		public string Name { get; }

		public string Version { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Name;
		#endregion
	}
}