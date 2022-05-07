namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	public class OneOffTemplateJob : TemplateJob
	{
		#region Constructors
		[JobInfo("One-Off Template Job")]
		public OneOffTemplateJob(JobManager jobManager)
				: base(jobManager)
		{
			this.Shuffle = !this.Site.EditingEnabled;
		}
		#endregion

		#region Public Properties
		public override string? LogDetails => "Update " + this.TemplateName;

		public override string LogName => "One-Off Template Job";
		#endregion

		#region Protected Override Properties
		protected override string EditSummary => "Update for changes in template";

		protected override string TemplateName => "Online Furnishing Summary";
		#endregion

		#region Protected Override Methods
		protected override void ParseTemplate(SiteTemplateNode template, ContextualParser parser)
		{
			template.Remove("luxury");
			template.Remove("master");
			template.Remove("collectible");
			template.Remove("houses");
			template.Remove("animated");
			template.Remove("interactable");
			template.Remove("light");
			template.Remove("lightcolour");
			template.RemoveIfValue("bindtype", v => string.Equals(v?.ToRaw().Trim(), "0", StringComparison.Ordinal));
			template.RenameParameter("other", "source");
			template.RenameParameter("tags", "behavior");
			template.RenameParameter("recipename", "planname");
			template.RenameParameter("recipequality", "planquality");
			template.RenameParameter("recipeid", "planid");
			template.RenameParameter("style", "theme");
			FixBehavior(template);
			this.FixList(template, "material");
			this.FixList(template, "skill");
			this.FixBundles(template);
		}
		#endregion

		#region Private Methods
		private static void FixBehavior(SiteTemplateNode template)
		{
			if (template.Find("behavior") is IParameterNode behavior)
			{
				var list = behavior.Value.ToRaw().Split(TextArrays.Comma);
				for (var i = 0; i < list.Length; i++)
				{
					if (list[i].StartsWith("Light ", StringComparison.OrdinalIgnoreCase))
					{
						list[i] = "Light";
					}
				}

				behavior.SetValue(string.Join(",", list), ParameterFormat.OnePerLine);
			}
		}

		private void FixBundles(SiteTemplateNode template)
		{
			if (template.Find("bundles") is IParameterNode bundles)
			{
				var value = bundles.Value;
				var factory = template.Factory;
				for (var i = 0; i < value.Count; i++)
				{
					if (value is ILinkNode link)
					{
						var siteLink = SiteLink.FromLinkNode(this.Site, link);
						value.RemoveAt(i);
						if (siteLink.Text is string text)
						{
							value.Insert(i, factory.TextNode(text));
						}
					}
				}
			}
		}

		private void FixList(SiteTemplateNode template, string parameterName)
		{
			var plural = parameterName + "s";
			if (template.Find(plural, parameterName) is IParameterNode param)
			{
				param.SetName(plural);
				var curValue = param.Value;
				var curText = curValue.ToRaw();
				var splitOn = curText.Contains('~', StringComparison.Ordinal) ? '~' : ',';
				var split = curText.Split(splitOn, StringSplitOptions.None);
				var list = new List<(string Name, int Value)>(split.Length / 2);
				for (var i = 0; i < split.Length; i += 2)
				{
					split[i + 1] = split[i + 1]
						.Replace(" ", string.Empty, StringComparison.Ordinal)
						.Replace("(", string.Empty, StringComparison.Ordinal)
						.Replace(")", string.Empty, StringComparison.Ordinal);
					var intValue = split[i + 1].Length == 0 ? 1 : int.Parse(split[i + 1], this.Site.Culture);
					list.Add((split[i], intValue));
				}

				if (string.Equals(parameterName, "material", StringComparison.Ordinal))
				{
					list.Sort((item1, item2) =>
						item2.Value.CompareTo(item1.Value) is int result && result == 0
							? string.Compare(item1.Name, item2.Name, false, this.Site.Culture)
							: result);
				}

				var sb = new StringBuilder(list.Count * 10);
				foreach (var (name, value) in list)
				{
					sb
						.Append(name)
						.Append('~')
						.Append(value.ToStringInvariant())
						.Append('~');
				}

				if (sb.Length > 0)
				{
					sb.Remove(sb.Length - 1, 1);
				}

				param.SetValue(sb.ToString(), ParameterFormat.OnePerLine);
			}
		}
		#endregion
	}
}