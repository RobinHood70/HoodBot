#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System.Collections.Generic;

public class ImageRotateInput : PageSetInput
{
	#region Constructors
	public ImageRotateInput(IEnumerable<string> titles, int rotation)
		: base(titles)
	{
		this.Rotation = rotation;
	}

	public ImageRotateInput(IGeneratorInput generatorInput, int rotation)
		: base(generatorInput)
	{
		this.Rotation = rotation;
	}

	public ImageRotateInput(IGeneratorInput generatorInput, IEnumerable<string> titles, int rotation)
		: base(generatorInput, titles)
	{
		this.Rotation = rotation;
	}

	protected ImageRotateInput(IEnumerable<long> ids, ListType listType, int rotation)
		: base(ids, listType)
	{
		this.Rotation = rotation;
	}

	protected ImageRotateInput(IGeneratorInput generatorInput, IEnumerable<long> ids, ListType listType, int rotation)
		: base(generatorInput, ids, listType)
	{
		this.Rotation = rotation;
	}
	#endregion

	#region Public Properties
	public int Rotation { get; }

	public string? Token { get; set; }
	#endregion

	#region Public Static Methods
	public static ImageRotateInput FromPageIds(IEnumerable<long> ids, int rotation) => new(ids, ListType.PageIds, rotation);

	public static ImageRotateInput FromPageIds(IGeneratorInput generatorInput, IEnumerable<long> ids, int rotation) => new(generatorInput, ids, ListType.PageIds, rotation);

	public static ImageRotateInput FromRevisionIds(IEnumerable<long> ids, int rotation) => new(ids, ListType.RevisionIds, rotation);

	public static ImageRotateInput FromRevisionIds(IGeneratorInput generatorInput, IEnumerable<long> ids, int rotation) => new(generatorInput, ids, ListType.RevisionIds, rotation);
	#endregion
}