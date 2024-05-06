namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.RequestBuilder;

	internal sealed class PropTemplates(WikiAbstractionLayer wal, TemplatesInput input, IPageSetGenerator? pageSetGenerator) : PropListModule<TemplatesInput, TemplatesResult, IApiTitle>(wal, input, pageSetGenerator), IGeneratorModule
	{
		#region Constructors
		public PropTemplates(WikiAbstractionLayer wal, TemplatesInput input)
			: this(wal, input, null)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 111;

		public override string Name => "templates";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "tl";
		#endregion

		#region Public Static Methods
		public static PropTemplates CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new(wal, (TemplatesInput)input, pageSetGenerator);

		public static PropTemplates CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new(wal, (TemplatesInput)input);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, TemplatesInput input)
		{
			ArgumentNullException.ThrowIfNull(request);
			ArgumentNullException.ThrowIfNull(input);
			request
				.Add("templates", input.Templates)
				.Add("namespace", input.Namespaces)
				.Add("dir", input.SortDescending)
				.Add("limit", this.Limit);
		}

		protected override IApiTitle GetItem(JToken result)
		{
			ArgumentNullException.ThrowIfNull(result);
			return result.GetWikiTitle();
		}

		protected override TemplatesResult GetNewList(JToken parent) => [];
		#endregion
	}
}