namespace RobinHood70.HoodBot.Jobs;

using System;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon.Parser;

public abstract class ParsedPageJob(JobManager jobManager) : EditJob(jobManager)
{
	#region Protected Abstract Methods
	protected abstract void ParseText(SiteParser parser);
	#endregion

	#region Protected Override Methods
	protected override void PageLoaded(Page page)
	{
		SiteParser parser = new(page);
		if (this.BotAllowed(parser))
		{
			this.ParseText(parser);
			parser.UpdatePage();
		}
	}

	private bool BotAllowed(SiteParser parser)
	{
		if (parser.FindTemplate("Bots") is not ITemplateNode botTemplate)
		{
			return true;
		}

		if (this.Site.User?.Name is not string botName)
		{
			// If there's a Bots template present but we're editing anonymously, deny access.
			return false;
		}

		if (botTemplate.Find("allow") is IParameterNode allowParam)
		{
			var split = allowParam.GetValue().Split(TextArrays.Comma, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (split.Contains(botName))
			{
				return true;
			}
		}

		if (botTemplate.Find("deny") is IParameterNode denyParam)
		{
			var split = denyParam.GetValue().Split(TextArrays.Comma, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (split.Contains(botName))
			{
				return false;
			}
		}

		if (botTemplate.Find("allowtasks") is IParameterNode allowTasksParam)
		{
			var split = allowTasksParam.GetValue().Split(TextArrays.Comma, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (split.Contains(this.JobName) || split.Contains(botName + '.' + this.JobName))
			{
				return true;
			}
		}

		if (botTemplate.Find("denytasks") is IParameterNode denyTasksParam)
		{
			var split = denyTasksParam.GetValue().Split(TextArrays.Comma, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (split.Contains(this.JobName) || split.Contains(botName + '.' + this.JobName))
			{
				return false;
			}
		}

		return false;
	}
	#endregion
}