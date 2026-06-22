namespace RobinHood70.HoodBot.Uesp;

using System;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;

internal static class MetaTemplateExtensions
{
	public static PageCollection GetMetaVariables(this Site site, PageModules pageModules, bool followRedirects, params string[] variables)
	{
		ArgumentNullException.ThrowIfNull(site);
		VariablesInput variablesInput = new() { Variables = variables };
		PageLoadOptions pageLoadOptions = new(pageModules, followRedirects);
		pageLoadOptions.CustomPropertyInputs.Add(variablesInput);

		return new PageCollection(site, pageLoadOptions);
	}
}