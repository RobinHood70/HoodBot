namespace RobinHood70.HoodBot.Jobs.Design
{
	using System;
	using static System.Environment;

	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
	public sealed class JobParameterFileAttribute : JobParameterAttribute
	{
		/// <summary>Gets or sets a value indicating whether to ignore the value in InitialFolder in favour of whatever folder the application specifies (or none).</summary>
		/// <value><see langword="true"/> if the application should specify the initial folder; otherwise, <see langword="false"/>.</value>
		/// <remarks>SpecialFolder does not provide a "None" value or similar, so this flag is provided if the application is expected to ignore the InitialFolder specified, possibly in favour of its own choice (or use it's own).</remarks>
		public bool IgnoreInitialFolder { get; set; } = true;

		public SpecialFolder InitialFolder { get; set; } = SpecialFolder.MyDocuments;

		public bool MustExist { get; set; }

		public bool Overwrite { get; set; }
	}
}