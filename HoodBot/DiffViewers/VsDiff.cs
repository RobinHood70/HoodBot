namespace RobinHood70.HoodBot.DiffViewers
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.IO;
	using System.Runtime.InteropServices;
	using EnvDTE;
	using RobinHood70.Robby;
	using static WikiCommon.Globals;

	[Description("Visual Studio")]
	public class VsDiff : IDiffViewer, IDisposable
	{
		#region Private Constants
		private const string VisualStudioProgID = "VisualStudio.DTE";
		#endregion

		#region Static Fields
		private static readonly Type VisualStudioType = Type.GetTypeFromProgID(VisualStudioProgID, true);
		private static DTE dte = GetDte();
		#endregion

		#region Fields
		private readonly List<Document> ourDocuments = new List<Document>();
		private bool disposedValue = false; // To detect redundant calls
		private Document lastDteDocument = null;
		#endregion

		#region Static Constructor
		static VsDiff() => Instance = new VsDiff();
		#endregion

		#region Constructors
		private VsDiff()
		{
			MessageFilter.Register();
			dte.Events.DocumentEvents.DocumentClosing += this.DteDocumentClosing;
		}
		#endregion

		#region Finalizers
		~VsDiff() => this.Dispose(false);
		#endregion

		#region Public Static Properties
		public static VsDiff Instance { get; }
		#endregion

		#region Public Methods
		public void Compare(Page page)
		{
			ThrowNull(page, nameof(page));
			var oldFile = Path.GetTempFileName();
			var newFile = Path.GetTempFileName();
			File.WriteAllText(oldFile, page.Revisions.Current?.Text ?? string.Empty);
			File.WriteAllText(newFile, page.Text);

			this.lastDteDocument = null;
			do
			{
				try
				{
					dte.ExecuteCommand("Tools.DiffFiles", $"\"{oldFile}\" \"{newFile}\" \"Revision as of {page.Revisions.Current?.Timestamp ?? DateTime.UtcNow}\" \"Latest revision\"");
					this.lastDteDocument = dte.ActiveDocument;
					dte.MainWindow.Visible = true;
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

		public void Wait()
		{
			var waiting = true;
			do
			{
				try
				{
					System.Threading.Thread.Sleep(500);
					waiting = (dte.MainWindow?.Visible ?? false) && this.lastDteDocument != null;
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
			if (!this.disposedValue)
			{
				if (dte.Documents.Count == 0)
				{
					dte.Quit();
				}
				else
				{
					Debug.WriteLine("Document Count: " + dte.Documents.Count);
					foreach (Document document in dte.Documents)
					{
						Debug.WriteLine(document.Name);
					}
				}

				MessageFilter.Revoke();
				dte = null;

				this.disposedValue = true;
			}
		}
		#endregion

		#region Private Static Methods
		private static DTE GetDte()
		{
			try
			{
				return Marshal.GetActiveObject(VisualStudioProgID) as DTE;
			}
			catch (COMException)
			{
			}

			return Activator.CreateInstance(VisualStudioType, true) as DTE;
		}
		#endregion

		#region Private Methods
		private void DteDocumentClosing(Document document)
		{
			Debug.WriteLine("Closing " + document.Name);
			if (document == this.lastDteDocument)
			{
				this.lastDteDocument = null;
			}

			this.ourDocuments.Remove(document);
		}
		#endregion
	}
}