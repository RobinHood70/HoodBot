#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;
	using RobinHood70.WallE.Eve.Modules;

	public class QueryInput : QueryPageSetInput
	{
		#region Constructors
		public QueryInput(params IQueryModule[] modules)
			: this(modules as IEnumerable<IQueryModule>)
		{
		}

		public QueryInput(IEnumerable<IQueryModule> queryModules) => this.PopulateModules(null, queryModules);

		public QueryInput(QueryPageSetInput pageSetInput, IEnumerable<IPropertyModule> propertyModules)
			: this(pageSetInput, propertyModules, null)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="QueryInput" /> class for combining multiple standard query modules with a pageset query module.</summary>
		/// <param name="pageSetInput">Page set information.</param>
		/// <param name="propertyModules">Property modules to use.</param>
		/// <param name="queryModules">Non-pageset query modules to use.</param>
		public QueryInput(QueryPageSetInput pageSetInput, IEnumerable<IPropertyModule> propertyModules, IEnumerable<IQueryModule>? queryModules)
			: base(pageSetInput)
		{
			this.PageSetQuery = true;
			this.PopulateModules(propertyModules, queryModules);
		}

		internal QueryInput(QueryInput input)
			: base(input)
		{
			this.GetInterwikiUrls = input.GetInterwikiUrls;
			this.QueryModules = input.QueryModules;
			this.PageSetQuery = input.PageSetQuery;
			this.PropertyModules = input.PropertyModules;
		}
		#endregion

		// All properties are internal because anything else risks modification in mid-query.
		#region Internal Properties
		internal IEnumerable<IQueryModule> AllModules
		{
			get
			{
				foreach (var module in this.QueryModules)
				{
					yield return module;
				}

				foreach (var module in this.PropertyModules)
				{
					yield return module;
				}
			}
		}

		internal bool GetInterwikiUrls { get; set; }

		internal bool PageSetQuery { get; }

		internal List<IPropertyModule> PropertyModules { get; } = new List<IPropertyModule>();

		internal List<IQueryModule> QueryModules { get; } = new List<IQueryModule>();
		#endregion

		#region Private Methods
		private void PopulateModules(IEnumerable<IPropertyModule>? propertyModules, IEnumerable<IQueryModule>? queryModules)
		{
			if (propertyModules != null)
			{
				foreach (var module in propertyModules)
				{
					if (module != null)
					{
						this.PropertyModules.Add(module);
					}
				}
			}

			// An empty query is still valid, so allow for that possibility.
			if (queryModules != null)
			{
				foreach (var module in queryModules)
				{
					if (module != null)
					{
						this.QueryModules.Add(module);
					}
				}
			}
		}
		#endregion
	}
}