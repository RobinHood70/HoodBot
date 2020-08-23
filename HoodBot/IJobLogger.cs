namespace RobinHood70.HoodBot
{
	/// <summary>Indicates that the class supports job logging.</summary>
	public interface IJobLogger
	{
		/// <summary>Gets the job logger.</summary>
		/// <value>The job logger.</value>
		/// <remarks>May be <see langword="null"/> if logging is not desired. Alternatively, you can set LogJobTypes to JobTypes.None.</remarks>
		JobLogger? JobLogger { get; }
	}
}
