namespace RobinHood70.Testing
{
	using System;
	using System.Diagnostics;
	using System.IO;
	using System.Text;
	using System.Windows.Forms;
	using RobinHood70.WikiClasses;

	public class WikiClassesTests : TestRunner
	{
		#region Constructors
		public WikiClassesTests(ITestForm parentForm, WikiInfo wikiInfo)
			: base(parentForm, wikiInfo)
		{
		}
		#endregion

		#region Static Tests
		public static void DebugReadTest()
		{
			// Note: this is not a full assertion test, or even part of one. It just spits out results to the Debug window/message box.
			var tests = new (bool hasHeader, string data)[]
			{
				(true, "header,data\n1\n2"), // Should give an error
				(false, "header, data\n1, 2"),
				(false, "header, data\n1, 2\n"),
				(true, "header, data\n1, 2"),
				(true, "header,data\n\"1\n2\", \" 3 \""),
			};

			foreach (var (hasHeader, data) in tests)
			{
				var csvFile = new CsvFile();
				using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
				using (var stream = new StreamReader(memoryStream))
				{
					try
					{
						csvFile.ReadStream(stream);
						csvFile.HasHeader = hasHeader;

						Debug.WriteLine("Header: " + (csvFile.HeaderRow == null ? "<null>" : string.Join(",", csvFile.HeaderRow)));
						Debug.WriteLine("Data Rows: " + csvFile.Count);
						foreach (var row in csvFile.DataRows)
						{
							Debug.WriteLine("  " + string.Join(",", row));
						}
					}
					catch (InvalidOperationException e)
					{
						MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
		}

		public static void FileWriteTest()
		{
			var csvFile = new CsvFile();
			csvFile.AddHeader(new[] { "Test", "Row" });
			csvFile.Add(new[] { "Simple Data", "Row 1" });
			csvFile.Add(new[] { "Split\nField", "Row 2" });
			csvFile.Add(new[] { "Quote\"Field", "Row 3" });
			csvFile.Add(new[] { "Comma,Field", "Row 4" });
			csvFile.WriteFile(@"D:\Data\HoodBot\Test.txt");
		}
		#endregion

		#region Public Override Methods
		public override void RunAll()
		{
			DebugReadTest();
			FileWriteTest();
		}

		public override void RunOne() => FileWriteTest();

		public override void Setup()
		{
		}

		public override void Teardown()
		{
		}
		#endregion
	}
}