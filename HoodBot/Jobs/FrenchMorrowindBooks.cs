namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Design;
using RobinHood70.HoodBot.Models;
using RobinHood70.HoodBot.Uesp;
using RobinHood70.HoodBot.ViewModels;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.WallE.Clients;
using RobinHood70.WallE.Eve;
using RobinHood70.WikiCommon.Parser;

internal sealed class FrenchMorrowindBooks : CreateOrUpdateJob<FrenchMorrowindBooks.Book>
{
	#region Private Constants
	private const string Query = "SELECT Files.fileid, filenamespace, name, edid, value, weight, scroll, icon, model, skill_lu, TEXT text FROM BOOK_Record INNER JOIN AllItems ON BOOK_Record.ordid = AllItems.ordid INNER JOIN Files ON AllItems.fileid = Files.fileid";
	private const string DbName = "CSData_MWFRData";
	private const string EditSummary = "Créer / mettre à jour le livre";
	private const string TemplateName = "Livre de jeu";
	#endregion

	#region Static Fields

	// private static readonly string[] EnglishSkills = { "Block", "Armorer", "Medium Armor", "Heavy Armor", "Blunt Weapon", "Long Blade", "Axe", "Spear", "Athletics", "Enchant", "Destruction", "Alteration", "Illusion", "Conjuration", "Mysticism", "Restoration", "Alchemy", "Unarmored", "Security", "Sneak", "Acrobatics", "Light Armor", "Short Blade", "Marksman", "Mercantile", "Speechcraft", "Hand-to-hand" };
	private static readonly string[] Skills = ["Parade", "Armurerie", "Armure intermédiaire", "Armure lourde", "Arme contondante", "Lame longue", "Hache", "Lance", "Athlétisme", "Enchantement", "Destruction", "Altération", "Illusion", "Invocation", "Mysticisme", "Guérison", "Alchimie", "Combat sans armure", "Sécurité", "Discrétion", "Acrobatie", "Armure légère", "Lame courte", "Précision", "Marchandage", "Eloquence", "Combat à mains nues"];
	#endregion

	#region Fields
	private readonly Regex brFixer = new(@"(\s*<br ?>){2,}\s*", RegexOptions.IgnoreCase, Globals.DefaultRegexTimeout);
	private readonly Regex divFinder = new(@"<DIV ALIGN=""CENTER"">(?<pre>\s*)(?<text>.*?)(?<post>\s*)((?=<DIV ALIGN=""[^""]+"">)|</DIV>|$)", RegexOptions.Singleline | RegexOptions.IgnoreCase, Globals.DefaultRegexTimeout);
	private readonly Regex fontFinder = new(@"<FONT( COLOR=""(?<color>[0-9A-F]+)"")?( SIZE=""(?<size>\d+)"")?( FACE=""(?<face>[^""]+)"")>(?<text>.*?)((?=<FONT )|</FONT>|$)", RegexOptions.Singleline | RegexOptions.IgnoreCase, Globals.DefaultRegexTimeout);
	private readonly Regex imgFinder = new(@"<IMG SRC(=|{{=}})""(?<filename>[^""]+?)"" WIDTH(=|{{=}})""(?<width>\d+)"" HEIGHT(=|{{=}})""(?<height>\d+)"">", RegexOptions.Singleline | RegexOptions.IgnoreCase, Globals.DefaultRegexTimeout);
	private readonly Regex tagFinder = new(@"<[^>]+>", RegexOptions.IgnoreCase, Globals.DefaultRegexTimeout);
	private readonly Dictionary<string, string> icons = new(StringComparer.Ordinal);
	#endregion

	#region Constructors
	[JobInfo("French Morrowind Books")]
	public FrenchMorrowindBooks(JobManager jobManager)
		: base(jobManager)
	{
		this.NewPageText = this.GetNewPageText;
		this.OnUpdate = this.UpdatePage;
		this.GetEnglishIcons(jobManager.Client);
	}
	#endregion

	#region Protected Override Methods
	protected override string? GetDisambiguator(Book item) => "book";

	protected override string GetEditSummary(Page page) => EditSummary;

	protected override bool IsValidPage(SiteParser parser, Book item) => parser.FindTemplate(TemplateName) is not null;

	protected override void LoadItems()
	{
		using var connection = new MySqlConnection(App.GetConnectionString("CSData"));
		connection.Open();
		connection.ChangeDatabase(DbName);
		foreach (var item in Database.RunQuery(connection, Query, -1, row => new Book(row)))
		{
			var title = TitleFactory.FromUnvalidated(this.Site, item.Namespace + ':' + item.Name);
			if (this.Items.TryAdd(title, item))
			{
				continue;
			}

			var existing = this.Items[title];
			if (item.MostlyEquals(existing))
			{
				foreach (var id in item.EditorIds)
				{
					existing.EditorIds.Add(id);
				}

				continue;
			}

			/* var newTitle = item.FileId == 0
				? null
				: this.TryAddItem(items, title.FullPageName() + " (" + item.Namespace + ")", item); */
			if (this.TryAddItem(this.Items, title.FullPageName() + " (" + item.EditorIds.First() + ")", item) is not Title newTitle)
			{
				throw new InvalidOperationException("No valid title was found for " + item.Name);
			}

			Debug.WriteLine("Disambiguated: " + newTitle.PageName);
		}
	}
	#endregion

	#region Private Static Methods
	private static string CenterDivReplacer(Match match) =>
		match.Groups["pre"].Value +
		"{{Centre|" + WikifyParameter(match.Groups["text"].Value) + "}}" +
		match.Groups["post"].Value;

	private static string FontReplacer(Match match)
	{
		if (!match.Groups["face"].Value.OrdinalICEquals("Daedric"))
		{
			return match.Groups["text"].Value;
		}

		var template = "{{PoliceDaedrique";
		if (match.Groups["size"].Success)
		{
			// var size = int.Parse(match.Groups["size"].Value);
			template += "|size=" + match.Groups["size"].Value;
		}

		return template + "|" + WikifyParameter(match.Groups["text"].Value) + "}}";
	}

	private static string ImageReplacer(Match match)
	{
		var fileName = match.Groups["filename"].Value;
		fileName = fileName
			.Replace(".tga", ".jpg", StringComparison.OrdinalIgnoreCase)
			.Replace(".bmp", ".jpg", StringComparison.OrdinalIgnoreCase);
		return "[[File:MW-" + fileName + "]]";
	}

	private static string WikifyParameter(string text) => text
		.Replace("=", "{{=}}", StringComparison.Ordinal)
		.Replace("|", "{{Pipe}}", StringComparison.Ordinal);
	#endregion

	#region Private Methods
	private void GetEnglishIcons(IMediaWikiClient client)
	{
		var wikis = App.UserSettings.Wikis;
		WikiInfoViewModel? uespWiki = null;
		foreach (var wiki in wikis)
		{
			if (wiki.DisplayName.OrdinalEquals("UESPWiki"))
			{
				uespWiki = wiki;
				break;
			}
		}

		if (uespWiki is null || uespWiki.Api is null)
		{
			throw new InvalidOperationException();
		}

		var api = new WikiAbstractionLayer(client, uespWiki.Api, this.Logger);
		api.SendingRequest += JobManager.WalSendingRequest;
		var enUesp = (UespSite)this.JobManager.CreateSite(uespWiki.WikiInfo, api, this.Site.EditingEnabled);
		enUesp.Login(uespWiki.UserName, uespWiki.Password);

		var pages = enUesp.CreateMetaPageCollection(PageModules.Info, false, "icon", "id", "id2", "id3", "id4", "id5");
		pages.SetLimitations(LimitationType.OnlyAllow, UespNamespaces.Morrowind, UespNamespaces.Bloodmoon, UespNamespaces.Tribunal);
		pages.GetBacklinks("Template:Game Book", WikiCommon.BacklinksTypes.EmbeddedIn, true, Filter.Exclude);
		foreach (var page in pages)
		{
			if (page is not VariablesPage varPage)
			{
				throw new InvalidOperationException();
			}

			if (varPage.GetVariable("icon") is not string icon)
			{
				continue;
			}

			foreach (var (key, value) in varPage.MainSet)
			{
				if (key.StartsWith("id", StringComparison.Ordinal))
				{
					var paramValue = value.Replace("<br>", "<br>", StringComparison.OrdinalIgnoreCase);
					var split = paramValue.Split("<br>", StringSplitOptions.RemoveEmptyEntries);
					foreach (var item in split)
					{
						this.icons.Add(item.Trim(), icon);
					}
				}
			}
		}

		api.SendingRequest -= JobManager.WalSendingRequest;
	}

	private string GetNewPageText(Title title, Book book) =>
		"{{" + TemplateName + '\n' +
		"}}\n" +
		this.Wikify(book.Text, title) +
		"\n{{Fin de livre}}";

	private Title? TryAddItem(IDictionary<Title, Book> items, string titleText, Book item)
	{
		var title = TitleFactory.FromUnvalidated(this.Site, titleText).ToTitle();
		return items.TryAdd(title, item)
			? title
			: null;
	}

	private void UpdatePage(SiteParser parser, Book book)
	{
		var template = parser.FindTemplate(TemplateName) ?? throw new InvalidOperationException();
		var index = 1;
		foreach (var id in book.EditorIds)
		{
			var name = index == 1
				? "id"
				: "id" + index.ToStringInvariant();
			template.AddIfNotExists(name, id, ParameterFormat.OnePerLine);
			if (this.icons.TryGetValue(id, out var icon))
			{
				template.Update("icon", icon);
			}
			else
			{
				Debug.WriteLine(id + " - " + parser.Page);
			}

			index++;
		}

		// var ns = this.nsInfo[book.Namespace];
		// template.AddIfNotExists(ns.Id, "1", ParameterFormat.OnePerLine);
		template.AddIfNotExists("value", book.Value.ToStringInvariant(), ParameterFormat.OnePerLine);
		template.AddIfNotExists("weight", book.Weight.ToStringInvariant(), ParameterFormat.OnePerLine);
		template.AddIfNotExists("lorename", "none", ParameterFormat.OnePerLine);
		if (book.Skill is ushort skill)
		{
			template.AddIfNotExists("skill", Skills[skill], ParameterFormat.OnePerLine);
		}

		if (book.IsScroll)
		{
			template.AddIfNotExists("scroll", "1", ParameterFormat.OnePerLine);
		}

		template.AddIfNotExists("description", string.Empty, ParameterFormat.OnePerLine);
	}

	private string Wikify(string text, Title title)
	{
		// CONSIDER: This would probably be better, albeit more complicated, as a parser. It would avoid a lot of weirdness around pipe and equals sign replacement.
		var originalText = text;

		if (title.PageNameEquals("36 Leçons de Vivec, 1er Sermon"))
		{
		}

		// Fixes
		text = text.Replace("</font<br>", "</font><br>", StringComparison.OrdinalIgnoreCase);
		text = text.Replace("<DIV ALIGN=«LEFT»>", "<DIV ALIGN=\"LEFT\">", StringComparison.OrdinalIgnoreCase);

		// Line breaks
		while (text.EndsWith("<br>", StringComparison.OrdinalIgnoreCase))
		{
			text = text[..^4].TrimEnd();
		}

		text = this.brFixer.Replace(text, this.WikifyBrs);
		text = text.Replace("<BR>", "<br>", StringComparison.OrdinalIgnoreCase);
		text = text.TrimEnd();

		// Font
		text = this.fontFinder.Replace(text, FontReplacer);

		// Alignment
		if (text.StartsWith("<DIV ALIGN=\"LEFT\">", StringComparison.OrdinalIgnoreCase))
		{
			text = text[18..];
		}

		text = text.Replace("<DIV ALIGN=\"CENTER\"><BR>", "<DIV ALIGN=\"CENTER\">", StringComparison.OrdinalIgnoreCase);
		text = this.divFinder.Replace(text, CenterDivReplacer);
		text = text.Replace("<DIV ALIGN=\"LEFT\">", string.Empty, StringComparison.OrdinalIgnoreCase);
		text = text.Replace("<DIV ALIGN=\"CENTER\">", string.Empty, StringComparison.OrdinalIgnoreCase);
		text = text.Replace("</DIV>", string.Empty, StringComparison.OrdinalIgnoreCase);

		text = text.Replace("{{Centre{{Pipe}}", "{{Centre|", StringComparison.Ordinal);
		text = text.Replace("{{PoliceDaedrique{{Pipe}}", "{{PoliceDaedrique|", StringComparison.Ordinal);

		// Images
		text = this.imgFinder.Replace(text, ImageReplacer);

		// Bolding
		text = text.Replace("<b>", "'''", StringComparison.OrdinalIgnoreCase);
		text = text.Replace("</b>", "'''", StringComparison.OrdinalIgnoreCase);

		// Show remaining
		var pause = false;
		foreach (Match match in this.tagFinder.Matches(text))
		{
			if (!string.Equals(match.Value, "<BR>", StringComparison.OrdinalIgnoreCase))
			{
				Debug.WriteLine(match.Value + ":  " + title.FullPageName());
				pause = true;
			}
		}

		if (false && pause)
		{
		}

		return text.Trim();
	}

	private string WikifyBrs(Match match) => new('\n', match.Value.AsSpan().Count('<'));
	#endregion

	#region Internal Classes
	internal sealed class Book
	{
		#region Constructors
		public Book(IDataRecord row)
		{
			var edid = (byte[])row["edid"];
			this.EditorIds.Add(Encoding.ASCII.GetString(edid).Trim());
			this.FileId = (byte)row["fileid"];
			this.Icon = ((string)row["icon"])[2..];
			this.IsScroll = (int)row["scroll"] == 1;
			this.Namespace = (string)row["filenamespace"];
			this.Model = ((string)row["model"])[2..];
			this.Name = (string)row["name"];
			if (row["skill_lu"] is ushort skill)
			{
				this.Skill = skill;
			}

			this.Text = row["text"] is byte[] textBytes
				? Encoding.UTF8.GetString(textBytes).Trim()
				: string.Empty;
			this.Value = (int)row["value"];
			this.Weight = (float)row["weight"];
		}
		#endregion

		#region Public Properties
		public SortedSet<string> EditorIds { get; } = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

		public int FileId { get; }

		public string Icon { get; }

		public bool IsScroll { get; }

		public string Model { get; }

		public string Name { get; }

		public string Namespace { get; }

		public ushort? Skill { get; }

		public string Text { get; }

		public int Value { get; }

		public float Weight { get; }
		#endregion

		#region Public Methods
		public bool MostlyEquals(Book other) =>
			this.FileId == other.FileId &&
			this.Icon.OrdinalEquals(other.Icon) &&
			this.IsScroll == other.IsScroll &&
			this.Name.OrdinalEquals(other.Name) &&
			this.Text.OrdinalEquals(other.Text) &&
			this.Value == other.Value &&
			this.Weight == other.Weight;
		#endregion
	}
	#endregion
}