#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using RobinHood70.WallE.Properties;
	using static RobinHood70.WikiCommon.Globals;

	public class SiteInfoNamespaceAlias
	{
		#region Constructors
		internal SiteInfoNamespaceAlias(int id, string alias)
		{
			this.Id = id;
			this.Alias = alias;
		}
		#endregion

		#region Public Properties
		public int Id { get; }

		public string Alias { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => CurrentCulture(Messages.ColonText, this.Id, this.Alias);
		#endregion
	}
}
