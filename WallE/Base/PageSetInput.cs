#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System;
using System.Collections.Generic;
using System.Globalization;
using RobinHood70.CommonCode;

public enum ListType
{
	Titles,
	PageIds,
	RevisionIds
}

public abstract class PageSetInput
{
	#region Fields
	private static readonly Dictionary<ListType, string> ListNames = new()
	{
		[ListType.PageIds] = "pageids",
		[ListType.RevisionIds] = "revids",
		[ListType.Titles] = "titles",
	};
	#endregion

	#region Constructors
	protected PageSetInput(PageSetInput input)
	{
		ArgumentNullException.ThrowIfNull(input);
		this.ConvertTitles = input.ConvertTitles;
		this.GeneratorInput = input.GeneratorInput;
		this.ListType = input.ListType;
		this.Redirects = input.Redirects;
		this.Values = input.Values;
	}

	protected PageSetInput(IEnumerable<string> titles)
	{
		ArgumentNullException.ThrowIfNull(titles);
		foreach (var title in titles)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(title);
		}

		this.ListType = ListType.Titles;
		var values = titles.AsReadOnlyList();
		this.Values = values;
	}

	protected PageSetInput(IGeneratorInput generatorInput)
	{
		ArgumentNullException.ThrowIfNull(generatorInput);
		this.GeneratorInput = generatorInput;
		this.Values = [];
	}

	protected PageSetInput(IGeneratorInput generatorInput, IEnumerable<string> titles)
		: this(titles)
	{
		ArgumentNullException.ThrowIfNull(generatorInput);
		this.GeneratorInput = generatorInput;
	}

	protected PageSetInput(IEnumerable<long> ids, ListType listType)
	{
		ArgumentNullException.ThrowIfNull(ids);
		this.ListType = listType;
		List<string> list = [];
		foreach (var id in ids)
		{
			list.Add(id.ToString(CultureInfo.InvariantCulture));
		}

		this.Values = list;
	}

	protected PageSetInput(IGeneratorInput generatorInput, IEnumerable<long> ids, ListType listType)
		: this(ids, listType)
	{
		this.GeneratorInput = generatorInput;
	}
	#endregion

	#region Public Static Properties
	public static ICollection<string> AllTypes { get; } = new HashSet<string>(ListNames.Values, StringComparer.Ordinal);
	#endregion

	#region Public Properties
	public bool ConvertTitles { get; set; }

	public IGeneratorInput? GeneratorInput { get; }

	public bool IsEmpty => this.Values.Count == 0 && this.GeneratorInput is null;

	public ListType ListType { get; }

	public bool Redirects { get; set; }

	public string TypeName => ListNames[this.ListType];

	public IReadOnlyList<string> Values { get; }
	#endregion
}