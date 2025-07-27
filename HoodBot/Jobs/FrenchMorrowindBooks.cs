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
using RobinHood70.HoodBot.Uesp;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon.Parser;

internal sealed class FrenchMorrowindBooks : CreateOrUpdateJob<FrenchMorrowindBooks.Book>
{
	#region Private Constants
	private const string Query = "SELECT Files.fileid, Files.filenamespace, name, edid, value, weight, scroll, icon, model, TEXT text FROM BOOK_Record INNER JOIN AllItems ON BOOK_Record.ordid = AllItems.ordid INNER JOIN Files ON AllItems.fileid = Files.fileid ORDER BY name";
	private const string DbName = "CSData_MWFRData";
	private const string EditSummary = "Créer / mettre à jour le livre";
	private const string LoreBookTemplate = "Livre du lore";
	private readonly Regex brFixer = new(@"(\s*<br>){2,}\s*", RegexOptions.IgnoreCase, Globals.DefaultRegexTimeout);
	private readonly UespNamespaceList nsInfo;
	#endregion

	#region Constructors
	[JobInfo("Books")]
	public FrenchMorrowindBooks(JobManager jobManager)
		: base(jobManager)
	{
		this.nsInfo = new UespNamespaceList(this.Site);
		this.NewPageText = this.GetNewPageText;
		this.OnUpdate = this.UpdatePage;
	}
	#endregion

	#region Protected Override Properties
	protected override string? Disambiguator => "book";
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => EditSummary;

	protected override bool IsValidPage(SiteParser parser, Book item) => parser.FindTemplate(LoreBookTemplate) is not null;

	protected override IDictionary<Title, Book> LoadItems()
	{
		var items = new Dictionary<Title, Book>();
		using var connection = new MySqlConnection(App.GetConnectionString("CSData"));
		connection.Open();
		connection.ChangeDatabase(DbName);
		foreach (var item in Database.RunQuery(connection, Query, -1, row => new Book(row)))
		{
			var title = TitleFactory.FromUnvalidated(this.Site, "Lore:" + item.Name);
			if (items.TryAdd(title, item))
			{
				continue;
			}

			var existing = items[title];
			var firstId = item.EditorIds.First();
			if (item.MostlyEquals(existing))
			{
				existing.EditorIds.Add(firstId);
				continue;
			}

			var newTitle = item.FileId == 0
				? null
				: this.TryAddItem(items, title.FullPageName() + " (" + item.Namespace + ")", item);
			newTitle ??= this.TryAddItem(items, title.FullPageName() + " (" + firstId + ")", item);
			if (newTitle is null)
			{
				throw new InvalidOperationException("No valid title was found for " + item.Name);
			}

			Debug.WriteLine("Disambiguated: " + newTitle.PageName);
		}

		return items;
	}
	#endregion

	#region Private Static Methods
	private Title? TryAddItem(IDictionary<Title, Book> items, string titleText, Book item)
	{
		var title = TitleFactory.FromUnvalidated(this.Site, titleText).ToTitle();
		return items.TryAdd(title, item)
			? title
			: null;
	}
	#endregion

	#region Private Methods

	private string GetNewPageText(Title title, Book book) =>
		"{{Livre du lore\n" +
		"|description=\n" +
		"}}\n" +
		this.Wikify(book.Text) +
		"\n{{Fin de livre}}";

	private void UpdatePage(SiteParser parser, Book book)
	{
		var template = parser.FindTemplate(LoreBookTemplate) ?? throw new InvalidOperationException();
		var ns = this.nsInfo[book.Namespace];
		template.AddIfNotExists(ns.Id, "1", ParameterFormat.OnePerLine);
	}

	private string Wikify(string text)
	{
		text = this.brFixer.Replace(text, this.WikifyBrs);
		return text;
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
			this.Icon = (string)row["icon"];
			this.IsScroll = (int)row["scroll"] == 1;
			this.Namespace = (string)row["filenamespace"];
			this.Model = (string)row["model"];
			this.Name = (string)row["name"];
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