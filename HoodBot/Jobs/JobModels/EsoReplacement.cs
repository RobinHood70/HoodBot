﻿namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using RobinHood70.WikiCommon.Parser;

	internal sealed class EsoReplacement(string from, NodeCollection to)
	{
		#region Public Properties
		public string From { get; } = from;

		public NodeCollection To { get; } = to;
		#endregion

		#region Public Override Methods
		public override string ToString() => this.From;
		#endregion
	}
}