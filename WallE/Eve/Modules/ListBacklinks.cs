﻿#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Properties;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.CommonCode.Globals;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class ListBacklinks : ListModule<BacklinksInput, BacklinksItem>, IGeneratorModule
	{
		#region Fields
		private readonly string prefix;
		private readonly string name;
		#endregion

		#region Contructors
		public ListBacklinks(WikiAbstractionLayer wal, BacklinksInput input)
			: this(wal, input, null)
		{
		}

		public ListBacklinks(WikiAbstractionLayer wal, BacklinksInput input, IPageSetGenerator? pageSetGenerator)
			: base(wal, input, pageSetGenerator) => (this.prefix, this.name) = input.LinkTypes switch
			{
				BacklinksTypes.Backlinks => ("bl", "backlinks"),
				BacklinksTypes.EmbeddedIn => ("ei", "embeddedin"),
				BacklinksTypes.ImageUsage => ("iu", "imageusage"),
				_ => throw new InvalidOperationException(CurrentCulture(input.LinkTypes.IsUniqueFlag() ? EveMessages.ParameterInvalid : EveMessages.InputNonUnique, nameof(ListAllLinks), input.LinkTypes))
			};
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 109;

		public override string Name => this.name;
		#endregion

		#region Protected Override Properties
		protected override string Prefix => this.prefix;
		#endregion

		#region Public Static Methods
		public static ListBacklinks CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) =>
			input is BacklinksInput listInput
				? new ListBacklinks(wal, listInput, pageSetGenerator)
				: throw InvalidParameterType(nameof(input), nameof(BacklinksInput), input.GetType().Name);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, BacklinksInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddIfNotNull("title", input.Title)
				.AddIf("pageid", input.PageId, input.Title == null)
				.Add("namespace", input.Namespace)
				.AddIf("dir", "descending", input.SortDescending)
				.AddIf("redirect", input.Redirect, input.LinkTypes != BacklinksTypes.EmbeddedIn)
				.AddFilterText("filterredir", "redirects", "nonredirects", input.FilterRedirects)
				.Add("limit", this.Limit);
		}

		protected override BacklinksItem? GetItem(JToken result)
		{
			if (result == null)
			{
				return null;
			}

			var redirects = new List<ITitle>();
			if (result["redirlinks"] is JToken redirLinks)
			{
				if (redirLinks.Type == JTokenType.Array)
				{
					foreach (var entry in redirLinks)
					{
						redirects.Add(entry.GetWikiTitle());
					}
				}
				else
				{
					// See https://phabricator.wikimedia.org/T73907
					foreach (var entry in redirLinks)
					{
						if (entry.First is JToken first)
						{
							redirects.Add(first.GetWikiTitle());
						}
					}
				}
			}

			return new BacklinksItem(
				ns: (int)result.MustHave("ns"),
				title: result.MustHaveString("title"),
				isRedirect: result["redirect"].GetBCBool(),
				pageId: (long)result.MustHave("pageid"),
				redirects: redirects,
				type: this.Input.LinkTypes);
		}

		// This module does some really funky things with the limits in redirect mode that don't interact well with how I'm dealing with limits. So, if a specific limit isn't set, always return "max" (-1) rather than the actual limit number, because otherwise the limit will actually take effect in an undesired way.
		protected override int GetNumericLimit() => this.Input.Redirect && this.Input.MaxItems == int.MaxValue ? -1 : base.GetNumericLimit();
		#endregion
	}
}
