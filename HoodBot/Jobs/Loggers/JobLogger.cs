namespace RobinHood70.HoodBot.Jobs.Loggers;

public abstract class JobLogger
{
	#region Public Abstract Methods

	/// <summary>Adds a new entry to the log.</summary>
	/// <param name="info">The log information.</param>
	public abstract void AddLogEntry(LogInfo info);
	#endregion

	#region Public Virtual Methods

	public virtual void CloseLog()
	{
	}

	/// <summary>Ends the log entry.</summary>
	/// <remarks>If necessary, log information can be updated at the end of the job.</remarks>
	public virtual void EndLogEntry()
	{
	}
	#endregion
}