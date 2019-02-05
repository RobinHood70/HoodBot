namespace RobinHood70.HoodBot.DiffViewers
{
	using System;
	using System.Diagnostics;
	using System.IO;
	using EnvDTE;
	using RobinHood70.Robby;

	public class VsDiff : IDiffViewer
	{
		#region Static Fields
		private static Type visualStudioType = Type.GetTypeFromProgID("VisualStudio.DTE");
		private static DTE globalDte = null;
		#endregion

		#region Fields
		private readonly string newFile;
		private readonly string oldFile;
		private Window dteWindow;
		#endregion

		#region Constructors
		public VsDiff(Page page)
		{
			if (globalDte == null)
			{
				globalDte = Activator.CreateInstance(visualStudioType) as DTE;
			}

			this.Page = page;
			this.oldFile = Path.GetTempFileName();
			this.newFile = Path.GetTempFileName();
		}
		#endregion

		#region Finalizers
		~VsDiff()
		{
			File.Delete(this.oldFile);
			File.Delete(this.newFile);
		}
		#endregion

		#region Public Properties
		public string Name { get; } = "Visual Studio";

		public Page Page { get; }
		#endregion

		#region Public Methods
		public void Compare()
		{
			File.WriteAllText(this.oldFile, this.Page.Revisions.Current.Text);
			File.WriteAllText(this.newFile, this.Page.Text);

			globalDte.ExecuteCommand("Tools.DiffFiles", $"\"{this.oldFile}\" \"{this.newFile}\" \"Revision as of {this.Page.Revisions.Current.Timestamp}\" \"Latest revision\"");
			this.dteWindow = globalDte.ActiveWindow;
			globalDte.Events.WindowEvents.WindowClosing += this.DteWindowClosing;
			globalDte.MainWindow.Visible = true;
		}

		public void Wait()
		{
			while ((globalDte.MainWindow?.Visible ?? false) && this.dteWindow != null)
			{
				System.Threading.Thread.Sleep(500);
			}
		}
		#endregion

		#region Private Methods
		private void DteWindowClosing(Window window)
		{
			Debug.WriteLine(window.Caption + window.WindowState.ToString());
			if (window == this.dteWindow)
			{
				this.dteWindow = null;
			}
		}
		#endregion
	}
}