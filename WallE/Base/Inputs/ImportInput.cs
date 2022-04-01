#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using RobinHood70.CommonCode;

	public class ImportInput
	{
		#region Fields
		private byte[] xmlData = Array.Empty<byte>();
		#endregion

		#region Constructors
		public ImportInput()
		{
		}

		public ImportInput(int ns)
		{
			this.Namespace = ns;
		}

		public ImportInput(string rootPage)
		{
			this.RootPage = rootPage.NotNullOrWhiteSpace();
		}
		#endregion

		#region Public Properties
		public bool FullHistory { get; set; }

		public string? InterwikiPage { get; set; }

		public string? InterwikiSource { get; set; }

		public int? Namespace { get; }

		public string? RootPage { get; }

		public string? Summary { get; set; }

		public bool Templates { get; set; }

		public string? Token { get; set; }
		#endregion

		#region Public Methods

		// Caller is responsible for proper encoding. Using methods instead of properties to avoid the issues with arrays as properties.
		public byte[] GetXmlData() => this.xmlData;

		public void SetXmlData(byte[] data) => this.xmlData = data;
		#endregion
	}
}
