namespace RobinHood70.VisualStudioDiff
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.IO;
	using System.Runtime.InteropServices;
	using EnvDTE;
	using RobinHood70.HoodBotPlugins;
	using RobinHood70.Robby;
	using static RobinHood70.WikiCommon.Globals;

	[Description("Visual Studio")]
	public class VsDiff : IDiffViewer, IDisposable
	{
		#region Private Constants
		private const int ComSleep = 1000;
		private const string VisualStudioProgID = "VisualStudio.DTE";
		#endregion

		#region Static Fields
		private static readonly object LockObject = new object();
		private static volatile DTE dte;
		#endregion

		#region Fields
		private readonly List<Window> ourWindows = new List<Window>();
		private bool disposed = false; // To detect redundant calls
		private Window lastDteWindow = null;
		#endregion

		#region Finalizers
		~VsDiff() => this.Dispose(false);
		#endregion

		#region Public Properties
		public string Name => Properties.Resources.Name;
		#endregion

		#region Private Properties
		private DTE Dte
		{
			get
			{
				// Check if dte is initialized so we're not locking unnecessarily.
				if (dte == null && !this.disposed)
				{
					lock (LockObject)
					{
						dte ??= GetDte(); // Check dte again in case of race condition while entering the lock, and the other guy won.

						// We have to watch windows rather than documents because diff documents are a weird hybrid that emits closing events for each separate document, neither of which corresponds to the ActiveDocument that we would've grabbed.
						dte.Events.WindowEvents.WindowClosing += this.DteWindowClosing;
					}
				}

				return dte;
			}
		}
		#endregion

		#region Public Methods
		public void Compare(Page page, string editSummary, bool isMinor, string editToken)
		{
			ThrowNull(page, nameof(page));
			var current = page.Revisions.Current;
			var oldFile = Path.GetTempFileName();
			var newFile = Path.GetTempFileName();
			File.WriteAllText(oldFile, current?.Text ?? " ");
			File.WriteAllText(newFile, page.Text ?? " ");

			var localDte = this.Dte;
			this.lastDteWindow = null;
			do
			{
				try
				{
					localDte.ExecuteCommand("Tools.DiffFiles", $"\"{oldFile}\" \"{newFile}\" \"Revision as of {current?.Timestamp ?? DateTime.UtcNow}\" \"{editSummary}\"");
					this.lastDteWindow = localDte.ActiveWindow;
					this.ourWindows.Add(this.lastDteWindow);
					localDte.MainWindow.Visible = true;
				}
				catch (COMException)
				{
					System.Threading.Thread.Sleep(ComSleep);
				}
			}
			while (this.lastDteWindow == null);

			File.Delete(oldFile);
			File.Delete(newFile);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		public bool ValidatePlugin() => Type.GetTypeFromProgID(VisualStudioProgID, false) != null;

		public void Wait()
		{
			var waiting = true;
			do
			{
				try
				{
					System.Threading.Thread.Sleep(ComSleep);
					waiting = (this.Dte.MainWindow?.Visible ?? false) && this.lastDteWindow != null;
				}
				catch (COMException)
				{
				}
			}
			while (waiting);

			if (this.lastDteWindow != null)
			{
				// If the app was closed rather than the document, close the document. The app doesn't actually close either way, but simply becomes invisible.
				this.lastDteWindow.Close(vsSaveChanges.vsSaveChangesNo);
			}
		}
		#endregion

		#region Protected Virtual Methods
		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				lock (LockObject)
				{
					var failed = true;
					while (failed)
					{
						try
						{
							if (dte != null && dte.Documents.Count == 0)
							{
								dte.Quit();
							}

							failed = false;
						}
						catch (COMException)
						{
							System.Threading.Thread.Sleep(ComSleep);
						}
					}

					dte = null;
					this.disposed = true;
				}
			}
		}
		#endregion

		#region Private Static Methods
		private static DTE GetDte()
		{
			var visualStudioType = Type.GetTypeFromProgID(VisualStudioProgID, true);
			/* Code below gets the active instance rather than creating a new one. Much faster for initial launch, but window close detection was occasionally unreliable. Testing with new project only to see if it's reliable that way. (And even if it's not, at least we can close the project.)
			try
			{
				return Marshal.GetActiveObject(VisualStudioProgID) as DTE;
			}
			catch (COMException)
			{
			}
			*/

			return Activator.CreateInstance(visualStudioType, true) as DTE;
		}
		#endregion

		#region Private Methods
		private void DteWindowClosing(Window window)
		{
			if (window == this.lastDteWindow)
			{
				this.lastDteWindow = null;
			}

			this.ourWindows.Remove(window);
		}
		#endregion
	}
}