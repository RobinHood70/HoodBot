namespace RobinHood70.Robby.Tests
{
	public partial class FormTestBed
	{
		/// <summary>Required designer variable.</summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>Clean up any resources being used.</summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.ComboBoxWiki = new System.Windows.Forms.ComboBox();
			this.textBoxResults = new System.Windows.Forms.TextBox();
			this.ButtonQuick = new System.Windows.Forms.Button();
			this.ButtonClear = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// ComboBoxWiki
			//
			this.ComboBoxWiki.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
			this.ComboBoxWiki.FormattingEnabled = true;
			this.ComboBoxWiki.Location = new System.Drawing.Point(15, 15);
			this.ComboBoxWiki.Name = "ComboBoxWiki";
			this.ComboBoxWiki.Size = new System.Drawing.Size(371, 21);
			this.ComboBoxWiki.TabIndex = 0;
			//
			// textBoxResults
			//
			this.textBoxResults.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxResults.Location = new System.Drawing.Point(15, 65);
			this.textBoxResults.Multiline = true;
			this.textBoxResults.Name = "textBoxResults";
			this.textBoxResults.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.textBoxResults.Size = new System.Drawing.Size(371, 181);
			this.textBoxResults.TabIndex = 5;
			//
			// ButtonQuick
			//
			this.ButtonQuick.Location = new System.Drawing.Point(15, 39);
			this.ButtonQuick.Margin = new System.Windows.Forms.Padding(0);
			this.ButtonQuick.Name = "ButtonQuick";
			this.ButtonQuick.Size = new System.Drawing.Size(83, 23);
			this.ButtonQuick.TabIndex = 1;
			this.ButtonQuick.Text = "Quick Test";
			this.ButtonQuick.UseVisualStyleBackColor = true;
			this.ButtonQuick.Click += new System.EventHandler(this.ButtonQuick_Click);
			//
			// ButtonClear
			//
			this.ButtonClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.ButtonClear.Location = new System.Drawing.Point(303, 39);
			this.ButtonClear.Margin = new System.Windows.Forms.Padding(0);
			this.ButtonClear.Name = "ButtonClear";
			this.ButtonClear.Size = new System.Drawing.Size(83, 23);
			this.ButtonClear.TabIndex = 4;
			this.ButtonClear.Text = "Clear Window";
			this.ButtonClear.UseVisualStyleBackColor = true;
			this.ButtonClear.Click += new System.EventHandler(this.ButtonClear_Click);
			//
			// FormTestBed
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(401, 261);
			this.Controls.Add(this.ButtonClear);
			this.Controls.Add(this.ButtonQuick);
			this.Controls.Add(this.textBoxResults);
			this.Controls.Add(this.ComboBoxWiki);
			this.MinimumSize = new System.Drawing.Size(275, 39);
			this.Name = "FormTestBed";
			this.Padding = new System.Windows.Forms.Padding(12);
			this.Text = "Robby Test Bed";
			this.Load += new System.EventHandler(this.FormTestBed_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
		private System.Windows.Forms.ComboBox ComboBoxWiki;
		private System.Windows.Forms.TextBox textBoxResults;
		private System.Windows.Forms.Button ButtonQuick;
		private System.Windows.Forms.Button ButtonClear;
	}
}
