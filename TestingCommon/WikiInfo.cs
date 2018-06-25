namespace RobinHood70.TestingCommon
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;

	public class WikiInfo
	{
		#region Constructors
		public WikiInfo(string tabSeparatedValues)
		{
			if (tabSeparatedValues == null)
			{
				throw new ArgumentNullException(nameof(tabSeparatedValues), "Value read from file is null. This should never happen.");
			}

			if (string.IsNullOrWhiteSpace(tabSeparatedValues))
			{
				return;
			}

			var split = tabSeparatedValues.Split(new char[] { '\t' }, StringSplitOptions.None);
			if (split.Length != 9)
			{
				throw new ArgumentException($"Incorrect number of values in tab-separated string: {tabSeparatedValues}");
			}

			this.Name = split[0];
			this.Uri = new Uri(split[1]);
			this.UserName = split[2];
			this.Password = split[3];
			this.AdminUserName = split[4].Length == 0 ? null : split[4];
			this.AdminPassword = split[5].Length == 0 ? null : split[5];
			this.ReadInterval = int.Parse(split[6], CultureInfo.InvariantCulture);
			this.WriteInterval = int.Parse(split[7], CultureInfo.InvariantCulture);
			this.SecretKey = split[8];
		}
		#endregion

		#region Public Properties
		public string AdminPassword { get; set; }

		public string AdminUserName { get; set; }

		public string Name { get; set; }

		public string Password { get; set; }

		public int ReadInterval { get; set; }

		public string SecretKey { get; set; }

		public Uri Uri { get; set; }

		public string UserName { get; set; }

		public int WriteInterval { get; set; }
		#endregion

		#region Public Static Methods
		public static IEnumerable<WikiInfo> LoadFile()
		{
			var retval = new List<WikiInfo>();
			foreach (var line in File.ReadAllLines("WikiList.txt"))
			{
				retval.Add(new WikiInfo(line));
			}

			return retval;
		}
		#endregion

		#region Public Override Methods
		public override string ToString() => $"{this.Name} ({this.UserName})";
		#endregion
	}
}