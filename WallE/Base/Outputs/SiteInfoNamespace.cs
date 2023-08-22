#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Properties;
	using RobinHood70.WikiCommon;

	public class SiteInfoNamespace
	{
		#region Constructors
		internal SiteInfoNamespace(int id, string canonicalName, string? defaultContentModel, NamespaceFlags flags, string name)
		{
			this.Id = id;
			this.CanonicalName = canonicalName;
			this.DefaultContentModel = defaultContentModel;
			this.Flags = flags;
			this.Name = name;
		}
		#endregion

		#region Public Properties
		public string CanonicalName { get; }

		public string? DefaultContentModel { get; }

		public NamespaceFlags Flags { get; }

		public int Id { get; }

		public string Name { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => Globals.CurrentCulture(Messages.ColonText, this.Id, this.Name);
		#endregion
	}
}