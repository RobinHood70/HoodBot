namespace RobinHood70.HoodBotPlugins
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel.Composition;
	using System.ComponentModel.Composition.Hosting;
	using System.ComponentModel.Composition.Primitives;
	using System.Diagnostics;
	using System.IO;
	using System.Reflection;

	/// <summary>A singleton class to handle all plugin functionality.</summary>
	public class Plugins : IDisposable
	{
		#region Fields
		private static Plugins instance;
		private static ComposablePartCatalog catalog;
		private static CompositionContainer container;
		#endregion

		#region Constructors
		private Plugins()
		{
			var folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			catalog = new DirectoryCatalog(folder + @"\Plugins\net48", "*Diff*.dll");
			container = new CompositionContainer(catalog);
			try
			{
				container.ComposeParts(this);
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

		/// <summary>Gets all plugins available.</summary>
		[ImportMany(typeof(IPlugin))]
		public IEnumerable<Lazy<IPlugin, IPluginMetadata>> All { get; private set; }

		/// <summary>Gets all difference viewers available.</summary>
		public IReadOnlyDictionary<string, IDiffViewer> DiffViewers { get; }
		#endregion

		#region Public Methods

		/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
		public void Dispose()
		{
			container.Dispose();
			catalog.Dispose();
		}
		#endregion
	}
}
