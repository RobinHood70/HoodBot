namespace RobinHood70.HoodBot.Uesp;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.WallE.Eve;

// TODO: Expand to add all namespaces and allow all namespace names (including aliases) as a lookup value.
public class UespNamespaceList : IReadOnlyDictionary<string, UespNamespace>
{
	#region Fields
	private readonly Dictionary<int, List<UespNamespace>> byNs = [];
	private readonly Dictionary<string, UespNamespace> byBase = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, UespNamespace> byId = new(StringComparer.OrdinalIgnoreCase);
	#endregion

	#region Constructors
	public UespNamespaceList(Site site)
	{
		ArgumentNullException.ThrowIfNull(site);
		if (site.AbstractionLayer is not WikiAbstractionLayer eve)
		{
			throw new InvalidOperationException();
		}

		var input = new NsinfoInput();
		var module = new ListNsinfo(eve, input);
		var nsInfo = eve.RunListQuery(module);
		foreach (var ns in nsInfo)
		{
			var uespNs = new UespNamespace(site, ns);
			this.byBase.Add(ns.Base, uespNs);
			if (!ns.Base.OrdinalICEquals(ns.Id))
			{
				this.byId.Add(ns.Id, uespNs);
			}

			if (!this.byNs.TryGetValue(uespNs.BaseNamespace.Id, out var list))
			{
				list = [];
				this.byNs.Add(uespNs.BaseNamespace.Id, list);
			}

			list.Add(uespNs);
		}

		foreach (var list in this.byNs.Values)
		{
			list.Sort(SortBase);
			if (list[^1].IsPseudoNamespace)
			{
				// Last list value must always be a base namespace.
				throw new InvalidOperationException();
			}
		}
	}
	#endregion

	#region Public Properties
	public int Count => this.byBase.Count;

	public IEnumerable<string> Keys => this.byBase.Keys;

	public IEnumerable<UespNamespace> Values => this.byBase.Values;
	#endregion

	#region Public Indexers
	public UespNamespace this[string key] => this.byBase.TryGetValue(key, out var retval)
		? retval
		: this.byId[key];
	#endregion

	#region Public Methods
	public bool ContainsKey(string key) => this.byBase.ContainsKey(key) || this.byId.ContainsKey(key);

	public UespNamespace? FromTitle(Title title)
	{
		var ns = title.Namespace.SubjectSpace;
		var subSpaces = this.byNs[ns.Id];
		var pageName = title.PageName;
		for (var i = 0; i < subSpaces.Count - 1; i++)
		{
			var uespNs = subSpaces[i];
			var modName = uespNs.ModName;
			if (pageName.Length > modName.Length && pageName[modName.Length] == '/')
			{
				pageName = pageName[..modName.Length];
			}

			if (ns.PageNameEquals(modName, pageName))
			{
				return uespNs;
			}
		}

		return subSpaces[^1];
	}

	public IEnumerator<KeyValuePair<string, UespNamespace>> GetEnumerator() => this.byBase.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => this.byBase.GetEnumerator();

	/// <summary>Returns the base if specified and found; otherwise, the base from the title.</summary>
	/// <param name="nsBase">The base to search for.</param>
	/// <param name="title">The title to use if base is null/not found.</param>
	/// <returns>The base requested or the base from the title.</returns>
	public UespNamespace? GetNsBase(string? nsBase, Title title) =>
		nsBase is not null &&
		this.TryGetValue(nsBase, out var retval)
			? retval
			: this.FromTitle(title);

	public bool TryGetValue(string key, [MaybeNullWhen(false)] out UespNamespace value) =>
		this.byBase.TryGetValue(key, out value) ||
		this.byId.TryGetValue(key, out value);
	#endregion

	#region Private Static Methods
	private static int SortBase(UespNamespace x, UespNamespace y) => -x.Base.Length.CompareTo(y.Base.Length); // No null checking since this is private.
	#endregion
}