namespace RobinHood70.HoodBot.Uesp
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using static RobinHood70.CommonCode.Globals;

	public class MetaNamespace : ISiteSpecific
	{
		#region Static Fields
		private static readonly Dictionary<string, MetaNamespace> NamespacesField = new Dictionary<string, MetaNamespace>(StringComparer.Ordinal);
		#endregion

		#region Constructors
		private MetaNamespace(Site site, string line)
		{
			this.Site = site ?? throw ArgumentNull(nameof(site));
			ThrowNull(line, nameof(line));
			var nsData = string.Concat(line, ";;;;;;").Split(TextArrays.Semicolon);
			for (var i = 0; i < nsData.Length; i++)
			{
				nsData[i] = nsData[i].Trim();
			}

			this.Base = nsData[0];
			var baseSplit = this.Base.Split(TextArrays.Colon, 2);
			this.BaseNamespace = site[baseSplit[0]];
			this.IsPseudoNamespace = baseSplit.Length == 2;
			this.Full = this.Base + (this.IsPseudoNamespace ? '/' : ':');
			this.Id = nsData[1].Length == 0 ? this.Base.ToUpperInvariant() : nsData[1];
			var parentName = nsData[2].Length == 0 ? this.Base : nsData[2];
			this.Parent = site[parentName];
			this.Name = nsData[3].Length == 0 ? this.Base : nsData[3];
			this.MainPage = Title.FromName(site, nsData[4].Length == 0 ? this.Full + this.Name : nsData[4]);
			this.Category = nsData[5].Length == 0 ? this.Base : nsData[5];
			this.Trail = nsData[6].Length == 0 ? string.Concat("[[", this.MainPage, "|", this.Name, "]]") : nsData[6];
		}
		#endregion

		#region Public Static Properties
		public static IReadOnlyDictionary<string, MetaNamespace> Namespaces => NamespacesField;
		#endregion

		#region Public Properties
		public string Base { get; }

		public Namespace BaseNamespace { get; }

		public string Category { get; }

		public string Full { get; }

		public string Id { get; }

		public bool IsPseudoNamespace { get; }

		public Title MainPage { get; }

		public string Name { get; }

		public Namespace Parent { get; }

		public Site Site { get; }

		public string Trail { get; }
		#endregion

		#region Public Static Methods
		public static MetaNamespace? FromTitle(Title title)
		{
			ThrowNull(title, nameof(title));
			var test = title.Namespace.DecoratedName + title.RootPageName;
			if (!Namespaces.TryGetValue(test, out var retval))
			{
				Namespaces.TryGetValue(title.Namespace.Name, out retval);
			}

			return retval;
		}

		public static MetaNamespace? ParentFromTitle(Title title)
		{
			var retval = FromTitle(title);
			return retval == null ? null : Namespaces[retval.Parent.Name];
		}

		public static void InitializeNamespaces(Site site)
		{
			// CONSIDER: Populating collection with all site namespaces, so it can respond as MetaTemplate would for those.
			ThrowNull(site, nameof(site));
			if (NamespacesField.Count == 0 && site.LoadMessage("Uespnamespacelist") is string message)
			{
				var lines = message.Split(TextArrays.LineFeed, StringSplitOptions.RemoveEmptyEntries);
				foreach (var line in lines)
				{
					if (line[0] != '<' && line[0] != '#')
					{
						var ns = new MetaNamespace(site, line);
						NamespacesField.Add(ns.Base, ns);
					}
				}
			}
		}
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Base;
		#endregion
	}
}