#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class DebugInfoInclude
	{
		#region Constructors
		public DebugInfoInclude(string name, string size)
		{
			this.Name = name;
			this.Size = size;
		}
		#endregion

		#region Public Properties
		public string Name { get; }

		public string Size { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => $"{this.Name} ({this.Size})";
		#endregion
	}
}
