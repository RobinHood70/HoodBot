namespace RobinHood70.HoodBot.Jobs.Tasks
{
	internal interface ITaskResult<T>
	{
		T Result { get; }
	}
}
