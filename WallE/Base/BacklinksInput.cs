#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System;
using RobinHood70.CommonCode;
using RobinHood70.WikiCommon;

public class BacklinksInput : ILimitableInput, IGeneratorInput
{
	#region Constructors
	public BacklinksInput(string title, BacklinksTypes linkTypes)
	{
		this.Title = title;
		this.LinkTypes = linkTypes;
	}

	public BacklinksInput(long pageId, BacklinksTypes linkTypes)
	{
		this.PageId = pageId;
		this.LinkTypes = linkTypes;
	}

	public BacklinksInput(BacklinksInput input, BacklinksTypes linkType)
	{
		ArgumentNullException.ThrowIfNull(input);
		this.FilterRedirects = input.FilterRedirects;
		this.Limit = input.Limit;
		this.MaxItems = input.MaxItems;
		this.PageId = input.PageId;
		this.Redirect = input.Redirect;
		this.SortDescending = input.SortDescending;
		this.Title = input.Title;
		this.Namespace = input.Namespace;
		this.LinkTypes = linkType;
	}
	#endregion

	#region Public Properties
	public Filter FilterRedirects { get; set; }

	public int Limit { get; set; }

	public BacklinksTypes LinkTypes { get; }

	public int MaxItems { get; set; }

	public int? Namespace { get; set; }

	public long PageId { get; set; }

	public bool Redirect { get; set; }

	public bool SortDescending { get; set; }

	public string? Title { get; set; }
	#endregion
}