﻿namespace RobinHood70.HoodBot.Uesp
{
	using System;
	using System.Collections.ObjectModel;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using static RobinHood70.CommonCode.Globals;

	// TODO: Expand to add all namespaces and allow all namespace names (including aliases) as a lookup value.
	public class UespNamespaceList : KeyedCollection<string, UespNamespace>
	{
		#region Constructors
		public UespNamespaceList(Site site)
		{
			// Add defined namespaces
			ThrowNull(site, nameof(site));
			if (site.LoadMessage("Uespnamespacelist") is string message)
			{
				var lines = message.Split(TextArrays.LineFeed, StringSplitOptions.RemoveEmptyEntries);
				foreach (var line in lines)
				{
					if (line[0] is not '<' and not '#')
					{
						this.Add(new UespNamespace(site, line));
					}
				}
			}

			// Add remaining namespaces
			foreach (var ns in site.Namespaces)
			{
				if (ns.Id >= 0 && !ns.IsTalkSpace && !this.Contains(ns.Name))
				{
					this.Add(new UespNamespace(site, ns.Name));
				}
			}
		}
		#endregion

		#region Public Methods

		public UespNamespace? FromTitle(Title title)
		{
			ThrowNull(title, nameof(title));
			var test = title.Namespace.DecoratedName + title.RootPageName;
			if (!this.TryGetValue(test, out var retval))
			{
				this.TryGetValue(title.Namespace.Name, out retval);
			}

			return retval;
		}

		public UespNamespace? ParentFromTitle(Title title)
		{
			var retval = this.FromTitle(title);
			return retval == null ? null : this[retval.Parent.Name];
		}
		#endregion

		#region Protected Override Methods

		protected override string GetKeyForItem(UespNamespace item) => (item ?? throw new InvalidOperationException()).Base;
		#endregion
	}
}
