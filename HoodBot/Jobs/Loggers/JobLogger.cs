namespace RobinHood70.HoodBot.Jobs.Loggers;

using System.Globalization;
using System.Resources;
using RobinHood70.HoodBot.Properties;

public abstract class JobLogger(CultureInfo? culture)
{
	#region Public Properties
	public CultureInfo Culture { get; } = culture ?? CultureInfo.CurrentUICulture;
	#endregion

	#region Protected Properties
	protected ResourceManager ResourceManager { get; } = new ResourceManager(typeof(Resources));
	#endregion

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