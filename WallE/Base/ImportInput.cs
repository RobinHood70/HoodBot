#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using static RobinHood70.WallE.ProjectGlobals;

	public class ImportInput
	{
		#region Constructors
		public ImportInput()
		{
		}

		public ImportInput(int ns) => this.Namespace = ns;

		public ImportInput(string rootPage)
		{
			ThrowNullOrWhiteSpace(rootPage, nameof(rootPage));
			this.RootPage = rootPage;
		}
		#endregion

		#region Public Properties
		public bool FullHistory { get; set; }

		public string InterwikiPage { get; set; }

		public string InterwikiSource { get; set; }

		public int? Namespace { get; }

		public string RootPage { get; }

		public string Summary { get; set; }

		public bool Templates { get; set; }

		public string Token { get; set; }

		public string Xml { get; set; }
		#endregion
	}
}
