#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Globalization;
	using static Properties.Messages;
	using static RobinHood70.Globals;

	#region Public Enumerations
	[Flags]
	public enum MessageFlags
	{
		None = 0,
		Customized = 1,
		DefaultMissing = 1 << 1,
		Missing = 1 << 2
	}
	#endregion

	public class AllMessagesItem : ITitleOnly
	{
		#region Public Properties
		public string Content { get; set; }

		public string Default { get; set; }

		public MessageFlags Flags { get; set; }

		public string Name { get; set; }

		public string NormalizedName { get; set; }

		public int? Namespace
		{
			get => (int?)DefaultNamespace.MediaWiki;
			set => throw new InvalidOperationException(CurrentCulture(NotSettable));
		}

		public string Title
		{
			get
			{
				// This is the inverse of the NormalizedName normalization process, making the Name property appear as it would in the page header and as most API calls would normalize it to anyway.
				var name = this.Name;
				if (!string.IsNullOrEmpty(name))
				{
					name = name.Replace('_', ' ');
					if (char.IsLower(name[0]))
					{
						// We have no knowledge of the wiki's culture, and doing so is complex and would probably be somewhat unreliable, so guess by using current culture.
						name = name.Length == 1 ? char.ToUpper(name[0], CultureInfo.CurrentCulture).ToString() : char.ToUpper(name[0], CultureInfo.CurrentCulture) + name.Substring(1);
					}
				}

				return "MediaWiki:" + name;
			}

			set => throw new InvalidOperationException(CurrentCulture(NotSettable));
		}
		#endregion
	}
}