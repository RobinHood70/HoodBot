namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
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
		private static readonly Regex BookMatcher = new(@"FormID: (?<formid>0x[0-9A-F]{8}) \(\d+\)\r\nEditorID: (?<edid>.*?)\r\n   Title: (?<title>.*?)\r\n(?<text>.*?)\r\n======================\r\n", RegexOptions.Singleline | RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
		#endregion

		#region Constructors
		[JobInfo("Books", "Starfield")]
		public SFBooks(JobManager jobManager)
			: base(jobManager)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		}
		#endregion

		#region Protected Override Properties
		protected override string? Disambiguator => "book";

		protected override string EditSummary => "Create book page";
		#endregion

		#region Protected Override Methods
		protected override bool IsValid(ContextualParser parser, Book item) => parser.FindSiteTemplate("Game Book") is not null;

		protected override IDictionary<Title, Book> LoadItems()
		{
			var retval = new Dictionary<Title, Book>();
			var books = LoadBooks();
			var csv = new CsvFile() { Encoding = Encoding.GetEncoding(1252) };
			csv.Load(LocalConfig.BotDataSubPath("Starfield/Books.csv"), true);
			foreach (var row in csv)
			{
				var formId = row["FormID"][2..];
				var book = books[formId];
				book.Value = int.Parse(row["Value"], CultureInfo.CurrentCulture);
				book.Weight = double.Parse(row["Weight"], CultureInfo.CurrentCulture);
				book.Model = row["Model"];
				var title = book.Title
					.Replace("#", string.Empty, StringComparison.Ordinal)
					.Replace("<Alias=", string.Empty, StringComparison.Ordinal)
					.Replace(">", string.Empty, StringComparison.Ordinal);
				retval.Add(TitleFactory.FromUnvalidated(this.Site, "Starfield:" + title), book);
			}

			return retval;
		}

		protected override string NewPageText(Title title, Book book)
		{
			var text =
				"{{Trail|Items|Books}}\n" +
				"{{Game Book\n";
			if (!string.Equals(book.Title, title.PageName, StringComparison.Ordinal))
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
		private static Dictionary<string, Book> LoadBooks()
		{
			var retval = new Dictionary<string, Book>(StringComparer.Ordinal);
			var fileText = File.ReadAllText(LocalConfig.BotDataSubPath("Starfield/Books.txt"), Encoding.GetEncoding(1252));
			var matches = BookMatcher.Matches(fileText) as IEnumerable<Match>;

			// In-file replacements prior to bot run:
			// * Some </font></b> where appropriate.
			// * Some </b></u> where appropriate.
			// * Trailing whitespace
			foreach (var item in matches)
			{
				var formId = item.Groups["formid"].Value[2..];

				// This is a horrible way to replace things, but it's simple and speed is not a factor for the relatively small number of books.
				var orig = item.Groups["text"].Value
					.Trim()
					.Replace("\r", string.Empty, StringComparison.Ordinal)
					.Replace("<b>", "'''", StringComparison.OrdinalIgnoreCase)
					.Replace("</b>", "'''", StringComparison.OrdinalIgnoreCase)
					.Replace("<i>", "''", StringComparison.OrdinalIgnoreCase)
					.Replace("</i>", "''", StringComparison.OrdinalIgnoreCase);
				var text = orig
					.Replace("\t", "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;", StringComparison.Ordinal)
					.Replace("<font size='20'>", "ƒspan style='font-size:50%'„", StringComparison.OrdinalIgnoreCase)
					.Replace("<font size='30'>", "ƒspan style='font-size:75%'„", StringComparison.OrdinalIgnoreCase)
					.Replace("<font size='50'>", "ƒspan style='font-size:125%'„", StringComparison.OrdinalIgnoreCase)
					.Replace("<font size='70'>", "ƒspan style='font-size:175%'„", StringComparison.OrdinalIgnoreCase)
					.Replace("<font size='80'>", "ƒspan style='font-size:200%'„", StringComparison.OrdinalIgnoreCase)
					.Replace("</font>", "ƒ/span„", StringComparison.OrdinalIgnoreCase)
					.Replace("<p align='center'>", "……Center†", StringComparison.OrdinalIgnoreCase)
					.Replace("</p>", "‡‡", StringComparison.OrdinalIgnoreCase)
					.Replace("<image name='BookImage_SlaytonLogo' caption='Slayton Aerospace'>", "[[File:Book-Slayton Logo|Slayton Aerospace]]", StringComparison.Ordinal);
				var text2 = Regex.Replace(text, @"[A-Za-z0-9\.,!?\ ():;'\""%=_&*#\[\]\|\\\/\@\$~`{}\r\nâèƒ„…†‡+-]+", string.Empty, RegexOptions.None, Globals.DefaultRegexTimeout);
				text = text
					.Replace('ƒ', '<')
					.Replace('„', '>')
					.Replace('…', '{')
					.Replace('†', '|')
					.Replace('‡', '}');
				if (text2.Length > 0)
				{
					Debug.WriteLine("=========================");
					Debug.WriteLine(text2);
					Debug.WriteLine(text);
					Debug.WriteLine(string.Empty);
				}

				if (!string.Equals(text, orig, StringComparison.Ordinal))
				{
					text += "\n[[Category:Needs Checking-Formatting]]";
				}

				var book = new Book(
					formId,
					item.Groups["edid"].Value,
					item.Groups["title"].Value,
					text,
					0,
					0,
					string.Empty);
				retval.Add(formId, book);
			}

			return retval;
		}
		#endregion

		#region Internal Classes
		internal record struct Book(string FormId, string EditorId, string Title, string Text, int Value, double Weight, string Model);
		#endregion
	}
}