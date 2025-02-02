﻿namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Jobs.JobModels;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;

internal sealed class SFBooks : CreateOrUpdateJob<SFBooks.Book>
{
	#region Static Fields
	// Used Editor IDs here instead of Form IDs since EDIDs are more likely to be unique across all mods.
	private static readonly Dictionary<string, string> TitleOverrides = new(StringComparer.Ordinal)
	{
		["LC180Slate_CF01A"] = "Trust Nobody (A)",
		["LC180Slate_CF01B"] = "Trust Nobody (B)",
		["MS04_SpacerSlate01COPY0000"] = "Lair Slate: The Gold Mine (Shattered Space)",
		["SFBGS001LC21_DataSlate_Warning"] = "Stolen Sandwich (Shattered Space)",
		["SFBGS001_Crater_Note06"] = "Happy Birthday (Shattered Space)",
		["SFBGS001_Dazra_AlterationClinic01"] = "Repair Notes (Shattered Space)",
		["SFBGS001_Dazra_TempleSlate04"] = "Thank you (template)",
		["SFBGS001_LC15Slate_Kalyn"] = "Thank you (Kalyn)",
		["SFBGS001_LC27_Checklist"] = "Checklist (Shattered Space)",
		["SFBGS001_LC48Slate_Lootlist"] = "Entrance and loot register",
		["SFBGS001_LC48Slate_Lootlist_quest"] = "Entrance and loot register (quest version)",
		["SFBGS001_MS04_Slate_Complete"] = "Sensor Data Collection Slate (complete)",
		["SFBGS001_VkaiZ03_Orphanage_AudioSlate02"] = "Orphanage: Chores (audio)",
		["SFBGS001_VkaiZ03_Orphanage_DataSlate02"] = "Orphanage: Chores (data)",
	};
	#endregion

	#region Constructors
	[JobInfo("Books", "Starfield")]
	public SFBooks(JobManager jobManager)
		: base(jobManager)
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		this.NewPageText = GetNewPageText;
	}
	#endregion

	#region Protected Override Properties
	protected override string? Disambiguator => "book";
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Create book page";

	protected override bool IsValid(SiteParser parser, Book item) => parser.FindTemplate("Game Book") is not null;

	protected override IDictionary<Title, Book> LoadItems()
	{
		var items = new Dictionary<Title, Book>();
		var csvName = GameInfo.Starfield.ModFolder + "Books.csv";
		if (!File.Exists(csvName))
		{
			return items;
		}

		var csv = new CsvFile(csvName)
		{
			Encoding = Encoding.GetEncoding(1252)
		};

		csv.HeaderFieldMap["Title"] = "Name";
		foreach (var row in csv.ReadRows())
		{
			var book = new Book(row);
			var titleText = TitleOverrides.GetValueOrDefault(book.EditorId, book.Title);
			var title = TitleFactory.FromUnvalidated(this.Site, "Starfield:" + titleText);
			items.Add(title, book);
		}

		return items;
	}
	#endregion

	#region Private Static Methods
	private static string GetNewPageText(Title title, Book book)
	{
		var text =
			"{{Trail|Items|Books}}\n" +
			"{{Game Book\n";
		if (!book.Title.OrdinalEquals(title.PageName))
		{
			text += "|linktitle=" + book.Title + "\n";
		}

		text +=
			"|lorename=none\n" +
			"|author=\n" +
			"|description=\n" +
			"|icon=\n" +
			$"|model={book.Model}\n" +
			$"|id={book.FormId}\n" +
			$"|eid={book.EditorId}\n" +
			$"|value={book.Value}\n" +
			$"|weight={book.Weight}\n" +
			"|questrel=\n" +
			"|loc=\n" +
			"|comment=\n" +
			"|note=\n" +
			"}}\n" +
			book.Text + "\n" +
			"{{Book End}}";

		return text;
	}
	#endregion

	#region Private Methods
	private static string GetBookText(string formId)
	{
		// This is a horrible way to replace things, but it's simple and speed is not a factor for the relatively small number of books.
		var orig = File.ReadAllText($@"{GameInfo.Starfield.ModFolder}Books\{formId}.txt", Encoding.GetEncoding(1252));
		if (orig.Length == 0)
		{
			return string.Empty;
		}

		if (orig[0] == '\t')
		{
			orig = ':' + orig[1..];
		}

		orig = orig
			.Replace("\r", string.Empty, StringComparison.Ordinal)
			.Replace("<b>", "'''", StringComparison.OrdinalIgnoreCase)
			.Replace("</b>", "'''", StringComparison.OrdinalIgnoreCase)
			.Replace("<i>", "''", StringComparison.OrdinalIgnoreCase)
			.Replace("</i>", "''", StringComparison.OrdinalIgnoreCase);
		var text = orig
			.Replace("\n\t", "\n:", StringComparison.Ordinal)
			.Replace("\t", "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;", StringComparison.Ordinal)
			.Replace("<font size='20'>", "<span style='font-size:50%'>", StringComparison.OrdinalIgnoreCase)
			.Replace("<font size='30'>", "<span style='font-size:75%'>", StringComparison.OrdinalIgnoreCase)
			.Replace("<font size='50'>", "<span style='font-size:125%'>", StringComparison.OrdinalIgnoreCase)
			.Replace("<font size='70'>", "<span style='font-size:175%'>", StringComparison.OrdinalIgnoreCase)
			.Replace("<font size='80'>", "<span style='font-size:200%'>", StringComparison.OrdinalIgnoreCase)
			.Replace("</font>", "</span>", StringComparison.OrdinalIgnoreCase)
			.Replace("<p align='center'>", "……Center†", StringComparison.OrdinalIgnoreCase)
			.Replace("</p>", "‡‡", StringComparison.OrdinalIgnoreCase)
			.Replace("<image name='BookImage_SlaytonLogo' caption='Slayton Aerospace'>", "[[File:Book-Slayton Logo|Slayton Aerospace]]", StringComparison.Ordinal);
		var textCheck = Regex.Replace(text, @"[A-Za-z0-9\.·,!?\ ():;'\""%=_&*#\[\]\|\\\/\@\$~`{}\n^é+-]+", string.Empty, RegexOptions.None, Globals.DefaultRegexTimeout);
		if (textCheck.Length > 0)
		{
			Debug.WriteLine("=========================");
			Debug.WriteLine(textCheck);
			Debug.WriteLine(orig);
			Debug.WriteLine(string.Empty);
		}

		if (!text.OrdinalEquals(orig))
		{
			text += "\n[[Category:Needs Checking-Formatting]]";
		}

		return text.Trim();
	}
	#endregion

	#region Internal Classes
	internal sealed class Book : SFItem
	{
		#region Constructors
		public Book(CsvRow row)
			: base(row, "Book")
		{
			this.Text = GetBookText(this.OriginalFormId);
			this.Title = this.Name
				.Replace("#", string.Empty, StringComparison.Ordinal)
				.Replace("<Alias=", string.Empty, StringComparison.Ordinal)
				.Replace(">", string.Empty, StringComparison.Ordinal);
		}
		#endregion

		#region Public Properties
		public string Text { get; }

		public string Title { get; }
		#endregion
	}
	#endregion
}