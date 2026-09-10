namespace RobinHood70.Robby;

using System;
using System.Collections.Generic;
using RobinHood70.WallE.Base;

/// <summary>Holds query page results.</summary>
public class QueryPageData
{
	#region Constructors

	/// <summary>Initializes a new instance of the <see cref="QueryPageData"/> class.</summary>
	/// <param name="item">The <see cref="QueryPageItem"/> to transform.</param>
	public QueryPageData(QueryPageItem item)
	{
		ArgumentNullException.ThrowIfNull(item);
		this.Value = item.Value;
		this.OtherData = item.DatabaseResult;
	}
	#endregion

	#region Public Properties

	/// <summary>Gets any other data the query page emits.</summary>
	/// <remarks>This is rare among query pages and the contents of the object will vary by page.</remarks>
	public IReadOnlyDictionary<string, object?>? OtherData { get; }

	/// <summary>Gets the value associated with the specific title.</summary>
	public string? Value { get; }
	#endregion
}