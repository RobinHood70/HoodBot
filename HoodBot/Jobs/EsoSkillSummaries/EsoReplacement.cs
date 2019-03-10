namespace RobinHood70.HoodBot.Jobs.EsoSkillSummaries
{
	using System;

	internal sealed class EsoReplacement : IComparable<EsoReplacement>
	{
		#region Constructors
		public EsoReplacement(string from, string to)
		{
			this.From = from;
			this.To = to;
		}
		#endregion

		#region Public Properties
		public string From { get; }

		public string To { get; }
		#endregion

		#region Public Methods
		public int CompareTo(EsoReplacement other) => string.Compare(this.From, other?.From, StringComparison.Ordinal);

		public string ReplaceFirst(string text)
		{
			// Replaces only the first index within the text
			var index = text.IndexOf(this.From, StringComparison.Ordinal);
			if (index < 0)
			{
				return text;
			}

			// Do not replace if already inside a link or template
			var indexLinkStart = text.Substring(0, index).LastIndexOf("[[", StringComparison.Ordinal);
			var indexLinkEnd = -1;
			if (indexLinkStart >= 0)
			{
				indexLinkEnd = text.IndexOf("]]", indexLinkStart, StringComparison.Ordinal);
				if (indexLinkStart < index && indexLinkEnd > index)
				{
					return text;
				}
			}

			var indexTemplateStart = text.Substring(0, index).LastIndexOf("{{", StringComparison.Ordinal);
			var indexTemplateEnd = -1;
			if (indexTemplateStart >= 0)
			{
				indexTemplateEnd = text.IndexOf("}}", indexTemplateStart, StringComparison.Ordinal);
				if (indexTemplateStart < index && indexTemplateEnd > index)
				{
					return text;
				}
			}

			return text.Substring(0, index) + this.To + text.Substring(index + this.From.Length);
		}
	}
	#endregion
}
