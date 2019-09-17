#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using RobinHood70.WallE.Properties;
	using static RobinHood70.WikiCommon.Globals;

	#region Public Enumerations
	[Flags]
	public enum NamespaceFlags
	{
		None = 0,
		CaseSensitive = 1,
		ContentSpace = 1 << 1,
		NonIncludable = 1 << 2,
		Subpages = 1 << 3
	}
	#endregion

	public class NamespacesItem
	{
		#region Public Properties
		public string CanonicalName { get; set; }

		public string DefaultContentModel { get; set; }

		public NamespaceFlags Flags { get; set; }

		public int Id { get; set; }

		public string Name { get; set; }
		#endregion

		#region Public Override Methods
		public override string ToString() => CurrentCulture(Messages.ColonText, this.Id, this.Name);
		#endregion
	}
}
