#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Base;
	using static ProjectGlobals;
	using static WikiCommon.Globals;

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "All inputs have Input suffix, even when collections.")]
	public class QueryInput : PageSetInput
	{
		#region Constructors
		public QueryInput(params IQueryModule[] modules)
			: this(modules as IEnumerable<IQueryModule>)
		{
		}

		public QueryInput(IEnumerable<IQueryModule> modules)
			: base() // Specified explicitly so VS can track the call, otherwise PageSetInput() appears unused when it's actually not.
		{
			ThrowNullRefCollection(modules, nameof(modules));
			foreach (var module in modules)
			{
				this.Modules.Add(module);
			}

			this.PropertyModules = new ModuleCollection<IPropertyModule>();
		}

		public QueryInput(WikiAbstractionLayer wal, PageSetInput pageSetInput, IEnumerable<IPropertyInput> propertyInputs)
			: base(pageSetInput)
		{
			ThrowNull(wal, nameof(wal));
			this.PropertyModules = wal.ModuleFactory.CreateModules(propertyInputs);
			this.PageSetQuery = true;
		}

		/// <summary>Initializes a new instance of the <see cref="QueryInput"/> class for combining multiple standard query modules with a pageset query module.</summary>
		/// <param name="wal">The calling abstraction layer.</param>
		/// <param name="pageSetInput">Page set information.</param>
		/// <param name="propertyInputs">Inputs for the property modules to use.</param>
		/// <param name="queryModules">Non-query modules to use.</param>
		public QueryInput(WikiAbstractionLayer wal, PageSetInput pageSetInput, IEnumerable<IPropertyInput> propertyInputs, IEnumerable<IQueryModule> queryModules)
			: this(wal, pageSetInput, propertyInputs)
		{
			ThrowNullRefCollection(queryModules, nameof(queryModules));
			foreach (var module in queryModules)
			{
				this.Modules.Add(module);
			}
		}

		internal QueryInput(QueryInput input)
			: base(input)
		{
			this.GetInterwikiUrls = input.GetInterwikiUrls;
			this.Modules = input.Modules;
			this.PageSetQuery = input.PageSetQuery;
			this.PropertyModules = input.PropertyModules;
		}
		#endregion

		#region Public Properties
		public IEnumerable<IQueryModule> AllModules
		{
			get
			{
				foreach (var module in this.Modules)
				{
					yield return module;
				}

				foreach (var module in this.PropertyModules)
				{
					yield return module;
				}
			}
		}

		public bool GetInterwikiUrls { get; set; }

		public ModuleCollection<IQueryModule> Modules { get; } = new ModuleCollection<IQueryModule>();

		public ModuleCollection<IPropertyModule> PropertyModules { get; }

		public bool PageSetQuery { get; }
		#endregion
	}
}