namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Collections.ObjectModel;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using System.Text;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;

	internal sealed partial class SFTerminals : CreateOrUpdateJob<SFTerminals.Terminal>
	{
		#region Fields
		private readonly Dictionary<string, Menu> menus = new(StringComparer.Ordinal);
		#endregion

		#region Constructors
		[JobInfo("Terminals", "Starfield")]
		public SFTerminals(JobManager jobManager)
			: base(jobManager)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			this.CreateOnly = Tristate.True;
			this.NewPageText = this.GetNewPageText;
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
			this.LoadMenus();
			return this.ReadTerm();
		}
		#endregion

		#region Private Static Methods

		private static string GlobalReplace(string text)
		{
			if (text is null || text.Length == 0)
			{
				return string.Empty;
			}

			text = Regex.Replace(text, @"(?<!\\n)\\n(?!\\n)", "<br>", RegexOptions.None, Globals.DefaultRegexTimeout);
			text = text
				.Replace("\\n", "\n", StringComparison.Ordinal)
				.Trim();
			text = Dewikify().Replace(text, "<nowiki/>$&");
			return text;
		}

		private static Dictionary<string, string> ReadDisambigs()
		{
			Dictionary<string, string> disambigs = new(StringComparer.Ordinal);
			var disambigLines = File.ReadAllLines(Starfield.ModFolder + "Term Disambigs.txt");
			foreach (var line in disambigLines)
			{
				var split = line.Split(TextArrays.Tab);
				disambigs.Add(split[0], split[1]);
			}

			return disambigs;
		}

		[GeneratedRegex(@"^[\*:;=]", RegexOptions.ExplicitCapture | RegexOptions.Multiline, 10000)]
		private static partial Regex Dewikify();
		#endregion

		#region Private Methods
		private string GetNewPageText(Title title, Terminal item)
		{
			Menu menu;
			if (item.MenuId.Length == 0)
			{
				menu = new Menu(item.EditorId, "NO MENUS", string.Empty, ImmutableDictionary<int, Entry>.Empty);
			}
			else
			{
				menu = this.menus.TryGetValue(item.MenuId, out var tryMenu)
					? tryMenu
					: new Menu(item.EditorId, "MENU NOT FOUND", item.MenuId, ImmutableDictionary<int, Entry>.Empty);
				if (menu.Entries.Count == 0)
				{
					this.Warn("MainMenu " + item.MenuId + " not found!");
				}
			}

			var menuList = new MenuList(menu);
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
				this.OutputMenu(sb, menuList, menuOffset);
				menuOffset++;
			}

			sb.Append("|}");
			return sb.ToString();
		}

		private void LoadMenus()
		{
			var csv = new CsvFile(Starfield.ModFolder + "Tmlm.csv")
			{
				Encoding = Encoding.GetEncoding(1252)
			};

			foreach (var row in csv.ReadRows())
			{
				var edid = row["EditorID"];
				if (!this.menus.TryGetValue(edid, out var menu))
				{
					var entries = new SortedDictionary<int, Entry>();
					menu = new Menu(edid, row["Full"], GlobalReplace(row["BTXT"]), entries);
					this.menus.Add(menu.EditorId, menu);
				}

				var entry = new Entry(
					row["ITXT"],
					row["ISTX"],
					int.Parse(row["ITID"], CultureInfo.CurrentCulture),
					row["TNAM"],
					GlobalReplace(row["UNAM"]) ?? string.Empty);
				menu.Entries.Add(int.Parse(row["Index"], CultureInfo.CurrentCulture), entry);
			}
		}

		private void OutputMenu(StringBuilder sb, MenuList menuList, int menuOffset)
		{
			var menu = menuList[menuOffset];
			if (menuOffset > 0)
			{
				sb.Append("|-\n");
			}

			sb
				.Append($"! colspan=2 | <span class=termHeader>")
				.Append(menu.Title.Length == 0 ? "&nbsp;" : menu.Title)
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

		private Dictionary<Title, Terminal> ReadTerm()
		{
			var disambigs = ReadDisambigs();
			var retval = new Dictionary<Title, Terminal>();
			var csv = new CsvFile(Starfield.ModFolder + "Term.csv")
			{
				Encoding = Encoding.GetEncoding(1252)
			};

			foreach (var row in csv.ReadRows())
			{
				var edid = row["EditorID"];
				var name = row["Full"];
				if (disambigs.TryGetValue(edid, out var disambig))
				{
					disambig = " (" + disambig + ")";
				}

				var title = TitleFactory.FromUnvalidated(this.Site[StarfieldNamespaces.Starfield], name + disambig);
				var image = row["Model"].Split(TextArrays.Backslash)[^1];
				var menuId = row["TMLM"];
				var terminal = new Terminal(name, edid, image, menuId);

				if (!retval.TryAdd(title, terminal))
				{
					Debug.WriteLine(edid + '\t' + name);
				}
			}

			return retval;
		}

		/*
		private void UpdateTerminal(ContextualParser parser, Terminal item)
		{
			parser.Clear();
			parser.AddText(this.GetNewPageText(parser.Title, item));
		}
		*/
		#endregion

		#region Internal Records
		internal sealed record Entry(string Heading, string AltTitle, int Sort2, string NextEntryId, string Text);

		internal sealed record Menu(string EditorId, string Title, string Instructions, IDictionary<int, Entry> Entries);

		internal sealed record Terminal(string Name, string EditorId, string ImageName, string MenuId);
		#endregion

		#region Internal Classes
		internal sealed class MenuList : KeyedCollection<string, Menu>
		{
			public MenuList(Menu menu)
				: base(StringComparer.Ordinal)
			{
				ArgumentNullException.ThrowIfNull(menu);
				this.Add(menu);
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