#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

public class CategoryInfoResult
{
	#region Constructors
	internal CategoryInfoResult(int files, int pages, int size, int subcategories, bool hidden)
	{
		this.Files = files;
		this.Pages = pages;
		this.Size = size;
		this.Subcategories = subcategories;
		this.Hidden = hidden;
	}
	#endregion

	#region Public Properties
	public int Files { get; }

	public bool Hidden { get; }

	public int Pages { get; }

	public int Size { get; }

	public int Subcategories { get; }
	#endregion
}