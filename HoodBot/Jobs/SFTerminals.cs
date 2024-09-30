namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Collections.ObjectModel;
	using System.Diagnostics;
	using System.Globalization;
	using System.Text;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;

	internal sealed class SFTerminals : CreateOrUpdateJob<SFTerminals.Terminal>
	{
		#region Fields
		private readonly MenuList menus = [];
		#endregion

		#region Constructors
		[JobInfo("Terminals", "Starfield")]
		public SFTerminals(JobManager jobManager)
			: base(jobManager)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		}
		#endregion

		#region Protected Override Properties
		protected override string? Disambiguator => "terminal";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Create terminal page";

		protected override bool IsValid(ContextualParser parser, Terminal item) => parser.FindSiteTemplate("Terminal Summary") is not null;

		protected override IDictionary<Title, Terminal> LoadItems()
		{
			var csv = new CsvFile() { Encoding = Encoding.GetEncoding(1252) };
			csv.Load(Starfield.ModFolder + "Tmlm.csv", true);
			Menu? menu = null;
			var lastId = string.Empty;
			var entries = new SortedDictionary<int, Entry>();
			foreach (var row in csv)
			{
				var edid = row["EditorID"];
				if (!string.Equals(edid, lastId, StringComparison.Ordinal))
				{
					if (menu is not null)
					{
						this.menus.Add(menu);
					}

					lastId = edid;
					entries = [];
					menu = new Menu(row["EditorID"], row["Full"], GlobalReplace(row["BTXT"]), entries);
				}

				var entry = new Entry(
					row["ITXT"],
					row["ISTX"],
					int.Parse(row["ITID"], CultureInfo.CurrentCulture),
					row["TNAM"],
					GlobalReplace(row["UNAM"]) ?? string.Empty);
				entries.Add(int.Parse(row["Index"], CultureInfo.CurrentCulture), entry);
			}

			if (menu is not null)
			{
				this.menus.Add(menu);
			}

			csv.Clear();

			var retval = new Dictionary<Title, Terminal>();
			csv.Load(Starfield.ModFolder + "Term2.csv", true);
			foreach (var row in csv)
			{
				var disambig = row["Disambig"];
				if (disambig.Length > 0)
				{
					disambig = " (" + disambig + ")";
				}

				var name = row["Full"];
				var title = TitleFactory.FromUnvalidated(this.Site, "Starfield:" + name + disambig);
				var image = row["Model"].Split(TextArrays.Backslash)[^1];
				var menuId = row["TMLM"];
				menu = this.menus.TryGetValue(menuId, out var tryMenu) ? tryMenu : new Menu(row["EditorID"], "MENU NOT FOUND", menuId, ImmutableDictionary<int, Entry>.Empty);
				var terminal = new Terminal(name, row["EditorID"], image, menu);
				if (menu.Entries.Count == 0)
				{
					Debug.WriteLine("MainMenu " + menuId + " not found!");
				}

				retval.Add(title, terminal);
			}

			return retval;

			static string GlobalReplace(string text)
			{
				if (text is null || text.Length == 0)
				{
					return string.Empty;
				}

				text = Regex.Replace(text, @"(?<!\\n)\\n(?!\\n)", "<br>", RegexOptions.None, Globals.DefaultRegexTimeout);
				text = text
					.Replace("\\n", "\n", StringComparison.Ordinal)
					.Trim();
				if (":;*=".Contains(text[0], StringComparison.Ordinal))
				{
					text = "<nowiki/>" + text;
				}

				return text;
			}
		}

		protected override string NewPageText(Title title, Terminal item)
		{
			var menuList = new MenuList()
			{
				item.MainMenu
			};
			var menuOffset = 0;
			var sb = new StringBuilder();
			sb.Append("{{Terminal Summary\n");
			if (!string.Equals(title.LabelName(), item.Name, StringComparison.Ordinal))
			{
				sb
					.Append($"|name={item.Name}\n");
			}

			sb
				.Append($"|edid={item.EditorId}\n")
				.Append($"|icon=<!--{item.ImageName}-->\n")
				.Append("|image=\n")
				.Append("|quest=\n")
				.Append("|location=\n")
				.Append("}}\n\n")
				.Append("<Add description of terminal>\n")
				.Append("== Menus ==\n")
				.Append("{| class=wikitable\n");

			while (menuOffset < menuList.Count)
			{
				OutputMenu(sb, menuList, menuOffset);
				menuOffset++;
			}

			sb.Append("|}");
			return sb.ToString();

			void OutputMenu(StringBuilder sb, MenuList menuList, int menuOffset)
			{
				var menu = menuList[menuOffset];
				if (menuOffset > 0)
				{
					sb.Append("|-\n");
				}

				sb
					.Append($"! colspan=2 | <span class=termHeader>")
					.Append(menu.Title)
					.Append("</span>\n");
				if (menu.Instructions.Length > 0)
				{
					sb
						.Append("|-\n")
						.Append("| colspan=2 | ")
						.Append(menu.Instructions)
						.Append('\n');
				}

				foreach (var kvp in menu.Entries)
				{
					var entry = kvp.Value;
					sb
						.Append("|-\n")
						.Append("| ")
						.Append(entry.Heading);
					if (entry.AltTitle.Length > 0)
					{
						sb.Append($" ({entry.AltTitle})");
					}

					sb.Append("\n| ");
					if (entry.NextEntryId.Length > 0)
					{
						this.menus.TryGetValue(entry.NextEntryId, out var nextMenu);
						var menuName = nextMenu?.Title ?? entry.NextEntryId;
						sb.Append($"(Go to <code>{menuName}<code> menu.)\n");
						if (nextMenu is not null && !menuList.Contains(entry.NextEntryId))
						{
							menuList.Add(nextMenu);
						}
					}
					else
					{
						sb
							.Append(entry.Text)
							.Append('\n');
					}
				}
			}
		}

		protected override void PageLoaded(ContextualParser parser, Terminal item)
		{
			parser.Clear();
			parser.AddText(this.NewPageText(parser.Title, item));
		}
		#endregion

		#region Internal Records
		internal sealed record Entry(string Heading, string AltTitle, int Sort2, string NextEntryId, string Text);

		internal sealed record Menu(string EditorId, string Title, string Instructions, IReadOnlyDictionary<int, Entry> Entries);

		internal sealed record Terminal(string Name, string EditorId, string ImageName, Menu MainMenu);
		#endregion

		#region Internal Classes
		internal sealed class MenuList : KeyedCollection<string, Menu>
		{
			public MenuList()
				: base(StringComparer.Ordinal)
			{
			}

			protected override string GetKeyForItem(Menu item)
			{
				ArgumentNullException.ThrowIfNull(item);
				return item.EditorId;
			}
		}
		#endregion
	}
}