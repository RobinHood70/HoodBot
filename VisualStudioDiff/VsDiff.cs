namespace RobinHood70.VisualStudioDiff
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.IO;
	using System.Runtime.InteropServices;
	using EnvDTE;
	using RobinHood70.HoodBotPlugins;

	[Description("Visual Studio")]
	public class VsDiff : IDiffViewer, IDisposable
	{
		#region Private Constants
		private const string VisualStudioProgID = "VisualStudio.DTE";
		#endregion

		#region Static Fields
		private static readonly object LockObject = new object();
		private static DTE dte;
		#endregion

		#region Fields
		private readonly List<Document> ourDocuments = new List<Document>();
		private bool disposed = false; // To detect redundant calls
		private Document lastDteDocument = null;
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
						MessageFilter.Register();
						dte = dte ?? GetDte(); // Check dte again in case of race condition while entering the lock, and the other guy won.
						dte.Events.DocumentEvents.DocumentClosing += this.DteDocumentClosing;
					}
				}

				return dte;
			}
		}
		#endregion

		#region Public Methods
		public void Compare(string oldText, string newText, string oldTitle, string newTitle)
		{
			var oldFile = Path.GetTempFileName();
			var newFile = Path.GetTempFileName();
			File.WriteAllText(oldFile, oldText ?? string.Empty);
			File.WriteAllText(newFile, newText);

			this.lastDteDocument = null;
			do
			{
				try
				{
					this.Dte.ExecuteCommand("Tools.DiffFiles", $"\"{oldFile}\" \"{newFile}\" \"{oldTitle}\" \"{newTitle}\"");
					this.lastDteDocument = this.Dte.ActiveDocument;
					this.Dte.MainWindow.Visible = true;
				}
				catch (COMException)
				{
					System.Threading.Thread.Sleep(500);
				}
			}
			while (this.lastDteDocument == null);

			this.ourDocuments.Add(this.lastDteDocument);
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
					System.Threading.Thread.Sleep(500);
					waiting = (this.Dte.MainWindow?.Visible ?? false) && this.lastDteDocument != null;
				}
				catch (COMException)
				{
				}
			}
			while (waiting);

			if (this.lastDteDocument != null)
			{
				this.lastDteDocument.Close(vsSaveChanges.vsSaveChangesNo);
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
					this.disposed = true;
					if (dte != null && dte.Documents.Count == 0)
					{
						dte.Quit();
					}

					dte = null;
					MessageFilter.Revoke();
				}
			}
		}
		#endregion

		#region Private Static Methods
		private static DTE GetDte()
		{
			var visualStudioType = Type.GetTypeFromProgID(VisualStudioProgID, true);
			try
			{
				return Marshal.GetActiveObject(VisualStudioProgID) as DTE;
			}
			catch (COMException)
			{
			}

			return Activator.CreateInstance(visualStudioType, true) as DTE;
		}
		#endregion

		#region Private Methods
		private void DteDocumentClosing(Document document)
		{
			if (document == this.lastDteDocument)
			{
				this.lastDteDocument = null;
			}

			this.ourDocuments.Remove(document);
		}
		#endregion
	}
}