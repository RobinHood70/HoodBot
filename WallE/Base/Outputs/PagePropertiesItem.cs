#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class PagePropertiesItem(string name, string value)
	{
		public string Name => name;

		public string Value => value;
	}
}