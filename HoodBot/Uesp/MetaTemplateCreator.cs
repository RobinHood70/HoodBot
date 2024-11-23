namespace RobinHood70.HoodBot.Uesp;

using System;
using System.Collections.Generic;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon;

public class MetaTemplateCreator : PageCreator
{
	#region Constructors
	public MetaTemplateCreator(PageCreator fallbackCreator, params string[] variables)
	{
		ArgumentNullException.ThrowIfNull(fallbackCreator);
		this.FallbackCreator = fallbackCreator;
		this.VariableNames = variables == null
			? []
			: new List<string>(variables);
	}
	#endregion

	#region Public Properties
	public PageCreator FallbackCreator { get; }

	public bool GameSpaceOnly { get; init; } = true;

	public IList<string> VariableNames { get; }
	#endregion

	#region Public Override Methods

	/// <inheritdoc/>
	public override Page CreatePage(Title title, PageLoadOptions options, IApiTitle? apiItem) => this.GameSpaceOnly && title.Namespace.Id < 100
		? this.FallbackCreator.CreatePage(title, options, apiItem)
		: new VariablesPage(title, options, apiItem);

	public override PageItem CreatePageItem(int ns, string title, long pageId, PageFlags flags) => new VariablesPageItem(ns, title, pageId, flags);
	#endregion

	#region Protected Override Methods
	protected override void AddCustomPropertyInputs(IList<IPropertyInput> propertyInputs)
	{
		VariablesInput variablesInput = new() { Variables = this.VariableNames };
		ArgumentNullException.ThrowIfNull(propertyInputs);
		propertyInputs.Add(variablesInput);
	}
	#endregion
}