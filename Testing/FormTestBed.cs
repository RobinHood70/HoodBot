namespace RobinHood70.Testing
{
	using System;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;
	using System.Windows.Forms;

	[SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "By design, though could potentially use a rewrite per TODO, below")]
	public partial class FormTestBed : Form, ITestForm
	{
		#region Fields
		private readonly Stopwatch sw = new Stopwatch();
		private int indent = 0;
		private int timePos;
		#endregion

		#region Constructors
		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Unless I'm missing something, I think CA is just confused here.")]
		public FormTestBed() => this.InitializeComponent();
		#endregion

		#region ITestForm Methods
		public void AppendResults(string message)
		{
			message = new string(' ', this.indent) + message + Environment.NewLine;
			if (this.textBoxResults.InvokeRequired)
			{
				this.textBoxResults.Invoke(new Action<string>(this.AppendResults), message);
			}
			else
			{
				this.textBoxResults.AppendText(message);
			}
		}

		public void ShowStopwatch()
		{
			this.indent -= 2;
			this.Insert($": {this.sw.ElapsedMilliseconds} ms");
			this.sw.Stop();
		}

		public void StartStopwatch(string testName)
		{
			this.AppendResults(testName);
			this.timePos = this.textBoxResults.Text.Length - 2;
			this.indent += 2;
			this.sw.Restart();
		}
		#endregion

		#region Form Event Handler Methods
		private void ButtonClear_Click(object sender, EventArgs e) => this.textBoxResults.Clear();

		private void ButtonQuick_Click(object sender, EventArgs e)
		{
			this.ButtonQuick.Enabled = false;
			ITestRunner test = new RobbyTests(this, this.ComboBoxWiki.SelectedItem as WikiInfo);
			var wikiInfo = this.ComboBoxWiki.SelectedItem as WikiInfo;
			test.Setup();
			test.RunOne();
			test.Teardown();
			this.ButtonQuick.Enabled = true;
		}

		private void ButtonRunAll_Click(object sender, EventArgs e)
		{
			this.ButtonRunAll.Enabled = false;
			ITestRunner tests = new WallETests(this, this.ComboBoxWiki.SelectedItem as WikiInfo);
			var wikiInfo = this.ComboBoxWiki.SelectedItem as WikiInfo;
			tests.Setup();
			tests.RunAll();
			tests.Teardown();
			this.ButtonRunAll.Enabled = true;
		}

		private void FormTestBed_Load(object sender, EventArgs e)
		{
			var allWikiInfo = WikiInfo.LoadFile();
			foreach (var item in allWikiInfo)
			{
				this.ComboBoxWiki.Items.Add(item);
			}

			if (this.ComboBoxWiki.Items.Count > 0)
			{
				this.ComboBoxWiki.SelectedIndex = 0;
			}
		}
		#endregion

		#region Private Methods
		private void Insert(string message)
		{
			this.textBoxResults.Text = this.textBoxResults.Text.Insert(this.timePos, message);
			this.textBoxResults.SelectionStart = this.textBoxResults.Text.Length;
			this.textBoxResults.ScrollToCaret();
		}
		#endregion
	}
}