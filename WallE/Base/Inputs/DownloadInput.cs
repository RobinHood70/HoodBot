#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class DownloadInput
	{
		public DownloadInput(string resource, string fileName)
		{
			this.FileName = fileName;
			this.Resource = resource;
		}

		public string FileName { get; }

		public string Resource { get; }
	}
}