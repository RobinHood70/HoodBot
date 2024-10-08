﻿namespace RobinHood70.HoodBot.Uesp
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;

	// TODO: Expand to add all namespaces and allow all namespace names (including aliases) as a lookup value.
	public class UespNamespaceList : KeyedCollection<string, UespNamespace>
	{
		#region Fields
		private readonly Dictionary<string, UespNamespace> nsIds = new(StringComparer.OrdinalIgnoreCase);
		#endregion

		#region Constructors
		public UespNamespaceList(Site site)
		{
			// TODO: Read from API, which will inherently include all namespaces.
			ArgumentNullException.ThrowIfNull(site);
			if (site.LoadMessage("nsinfo-namespacelist") is string message)
			{
				var idTag = Regex.Match(message, @"{\|.*?\bid=(?<delim>['""]?)nsinfo-table\k<delim>\b(.|\n)*?\|-(?<text>(.|\n)*?)\|}", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
				var lines = idTag.Groups["text"].Value.Split("\n|-", StringSplitOptions.RemoveEmptyEntries);
				foreach (var line in lines)
				{
					var row = line.Split(TextArrays.NewLineChars, 2)[^1];
					if (row.Length > 0 && row[0] == '|')
					{
						row = row[1..];
						var nsData = new UespNamespace(site, row);
						this.Add(nsData);
						this.nsIds.Add(nsData.Id, nsData);
					}
				}
			}

			// Add remaining namespaces
			foreach (var ns in site.Namespaces)
			{
				if (!this.Contains(ns.Name))
				{
					// Second ns.Name is to ensure mixed case for backwards compatibility
					this.Add(new UespNamespace(site, $"{ns.Name} || {ns.Name} || || || || || ||"));
				}
			}
		}
		#endregion

		#region Public Methods

		public UespNamespace? FromId(string id) => this.nsIds.TryGetValue(id, out var retval)
			? retval
			: null;

		public UespNamespace? FromTitle(Title title)
		{
			// TODO: Optimize this more like it is in NSInfo itself.
			var ns = title.Namespace.SubjectSpace;
			var comparer = title.Site.GetPageNameComparer(ns.CaseSensitive);
			var subSpaces = this.Items
				.Where(ns => ns.IsPseudoNamespace && ns.BaseNamespace == title.Namespace)
				.OrderByDescending(ns => ns.ModName.Length);
			foreach (var uespNs in subSpaces)
			{
				var pageName = title.PageName;
				if (pageName.Length > uespNs.ModName.Length && pageName[uespNs.ModName.Length] == '/')
				{
					pageName = pageName[0..(uespNs.ModName.Length - 1)];
				}

				if (comparer.Equals(uespNs.ModName, pageName))
				{
					return uespNs;
				}
			}

			return this[title.Namespace.CanonicalName];
		}

		public UespNamespace? GetAnyBase(string? nsBase) =>
			nsBase is null ? null :
			this.TryGetValue(nsBase, out var retval) ? retval :
			this.FromId(nsBase);

		public UespNamespace? GetNsBase(Title title, string? nsBase) =>
			this.GetAnyBase(nsBase) ?? this.FromTitle(title);

		public Namespace? ParentFromTitle(Title title) => this.FromTitle(title)?.Parent;
		#endregion

		#region Protected Override Methods

		protected override string GetKeyForItem(UespNamespace item) => (item ?? throw new InvalidOperationException()).Base;
		#endregion
	}
}