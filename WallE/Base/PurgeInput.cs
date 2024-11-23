#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System.Collections.Generic;
using RobinHood70.WikiCommon;

public class PurgeInput : PageSetInput
{
	#region Constructors
	public PurgeInput(IEnumerable<string> titles, PurgeMethod method)
		: base(titles)
	{
		this.Method = method;
	}

	public PurgeInput(IGeneratorInput generatorInput, PurgeMethod method)
		: base(generatorInput)
	{
		this.Method = method;
	}

	public PurgeInput(IGeneratorInput generatorInput, IEnumerable<string> titles, PurgeMethod method)
		: base(generatorInput, titles)
	{
		this.Method = method;
	}

	protected PurgeInput(IEnumerable<long> ids, ListType listType, PurgeMethod method)
		: base(ids, listType)
	{
		this.Method = method;
	}

	protected PurgeInput(IGeneratorInput generatorInput, IEnumerable<long> ids, ListType listType, PurgeMethod method)
		: base(generatorInput, ids, listType)
	{
		this.Method = method;
	}
	#endregion

	#region Public Properties
	public PurgeMethod Method { get; }
	#endregion

	#region Public Static Methods
	public static PurgeInput FromPageIds(IEnumerable<long> ids, PurgeMethod method) => new(ids, ListType.PageIds, method);

	public static PurgeInput FromPageIds(IGeneratorInput generatorInput, IEnumerable<long> ids, PurgeMethod method) => new(generatorInput, ids, ListType.PageIds, method);

	public static PurgeInput FromRevisionIds(IEnumerable<long> ids, PurgeMethod method) => new(ids, ListType.RevisionIds, method);

	public static PurgeInput FromRevisionIds(IGeneratorInput generatorInput, IEnumerable<long> ids, PurgeMethod method) => new(generatorInput, ids, ListType.RevisionIds, method);
	#endregion
}