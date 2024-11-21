#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class SlotInput(string name)
	{
		public string? ContentFormat { get; set; }

		public string Name { get; set; } = name;
	}
}