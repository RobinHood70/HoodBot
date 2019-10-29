namespace RobinHood70.HoodBotPlugins
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel.Composition;
	using System.ComponentModel.Composition.Hosting;
	using System.ComponentModel.Composition.Primitives;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;
	using System.IO;
	using System.Reflection;

	/// <summary>A singleton class to handle all plugin functionality.</summary>
	public class Plugins : IDisposable
	{
		#region Static Fields
		private static Plugins? instance;
		#endregion

		#region Fields
		private readonly ComposablePartCatalog? catalog;
		private readonly CompositionContainer? container;
		private bool disposedValue = false; // To detect redundant calls
		#endregion

		#region Constructors
		private Plugins()
		{
			var folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			this.catalog = new DirectoryCatalog(folder + @"\Plugins\net48", "*Diff*.dll");
			this.container = new CompositionContainer(this.catalog);
			try
			{
				this.container.ComposeParts(this);
			}
			catch (CompositionException ce)
			{
				Debug.WriteLine(ce.Message);
			}

			var diffViewers = new Dictionary<string, IDiffViewer>();
			foreach (var plugin in this.All)
			{
				if (plugin.Value is IDiffViewer diffViewer)
				{
					diffViewers.Add(plugin.Metadata.DisplayName, diffViewer);
				}
			}

			this.DiffViewers = diffViewers;
		}
		#endregion

		#region Public Static Properties

		/// <summary>Gets the single instance of the class.</summary>
		/// <value>The instance.</value>
		public static Plugins Instance => instance ??= new Plugins();
		#endregion

		#region Public Properties

		/// <summary>Gets all plugins available. Initialized to non-null via <see cref="ImportManyAttribute"/> in <see cref="System.ComponentModel.Composition"/>.</summary>
		[ImportMany(typeof(IPlugin))]
		[NotNull]
		public IEnumerable<Lazy<IPlugin, IPluginMetadata>>? All { get; private set; }

		/// <summary>Gets all difference viewers available.</summary>
		public IReadOnlyDictionary<string, IDiffViewer> DiffViewers { get; }
		#endregion

		#region Public Methods

		/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion

		#region Protected Methods

		/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
		/// <param name="disposing"><see langword="true"/> if the object is being disposed; <see langword="false"/> if it's finalizing.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposedValue)
			{
				if (disposing)
				{
					this.catalog?.Dispose();
					this.container?.Dispose();
				}

				this.disposedValue = true;
			}
		}
		#endregion
	}
}