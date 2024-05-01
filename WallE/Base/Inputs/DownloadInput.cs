#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class DownloadInput(string resource, string? fileName)
	{
		public string? FileName { get; } = fileName;

		public string Resource { get; } = resource;
	}
}