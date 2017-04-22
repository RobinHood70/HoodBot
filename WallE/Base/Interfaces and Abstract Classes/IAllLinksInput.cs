#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public interface IAllLinksInput : ILimitableInput, IGeneratorInput
	{
		string From { get; set; }

		AllLinksTypes LinkType { get; }

		int? Namespace { get; }

		string Prefix { get; set; }

		AllLinksProperties Properties { get; set; }

		bool SortDescending { get; set; }

		string To { get; set; }

		bool Unique { get; set; }
	}
}