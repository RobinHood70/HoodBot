namespace RobinHood70.Robby.Design;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using RobinHood70.Robby;

/// <summary>Provides a debugger view for a collection of <see cref="ITitle"/>s.</summary>
/// <remarks>This type is intended for use by debugger visualizers and is not intended for direct use in
/// application code. When inspecting a <see cref="ICollection{TTitle}"/> in the debugger, the items are shown as an
/// array for improved readability.</remarks>
/// <typeparam name="T">The type of title contained in the collection. Must implement <see cref="ITitle"/>.</typeparam>
public sealed class TitleCollectionDebugView<T>
	where T : ITitle
{
	private readonly ICollection<T> collection;

	/// <summary>Initializes a new instance of the <see cref="TitleCollectionDebugView{T}"/> class.</summary>
	/// <param name="collection">The collection to be viewed.</param>
	public TitleCollectionDebugView(ICollection<T> collection)
	{
		ArgumentNullException.ThrowIfNull(collection);
		this.collection = collection;
	}

	/// <summary>Gets the items in the collection as an array.</summary>
	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	[SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "This property is required for debugger visualization\r\n\t/// and is not intended for use in application code.")]
	public T[] Items
	{
		get
		{
			var retval = new T[this.collection.Count];
			this.collection.CopyTo(retval, 0);
			return retval;
		}
	}
}