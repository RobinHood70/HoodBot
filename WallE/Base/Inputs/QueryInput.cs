#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;
	using RobinHood70.WallE.Eve.Modules;

	public class QueryInput : QueryPageSetInput
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="QueryInput" /> class for a standard pageset query module.</summary>
		/// <param name="pageSetInput">Page set information.</param>
		/// <param name="propertyModules">Property modules to use.</param>
		public QueryInput(QueryPageSetInput pageSetInput, IEnumerable<IPropertyModule> propertyModules)
			: base(pageSetInput)
		{
			this.PropertyModules.AddRange(propertyModules);
		}

		/// <summary>Initializes a new instance of the <see cref="QueryInput" /> class for combining multiple standard query modules with a pageset query module.</summary>
		/// <param name="pageSetInput">Page set information.</param>
		/// <param name="propertyModules">Property modules to use.</param>
		/// <param name="queryModules">Query modules to use.</param>
		public QueryInput(QueryPageSetInput pageSetInput, IEnumerable<IPropertyModule> propertyModules, IEnumerable<IQueryModule> queryModules)
			: this(pageSetInput, propertyModules)
		{
			this.QueryModules.AddRange(queryModules);
		}

		internal QueryInput(QueryInput input)
			: base(input)
		{
			this.GetInterwikiUrls = input.GetInterwikiUrls;
			this.PropertyModules = input.PropertyModules;
		}
		#endregion

		// All properties are internal because anything else risks modification in mid-query.
		#region Internal Properties
		internal bool GetInterwikiUrls { get; set; }

		internal List<IPropertyModule> PropertyModules { get; } = new List<IPropertyModule>();

		internal List<IQueryModule> QueryModules { get; } = new List<IQueryModule>();
		#endregion
	}
}