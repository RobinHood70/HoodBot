#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class SiteInfoLanguage
	{
		#region Constructors
		internal SiteInfoLanguage(string code, string name)
		{
			this.Code = code;
			this.Name = name;
		}
		#endregion

		#region Public Properties
		public string Code { get; }

		public string Name { get; }
		#endregion

		#region Public Override Method
		public override string ToString() => this.Code;
		#endregion
	}
}