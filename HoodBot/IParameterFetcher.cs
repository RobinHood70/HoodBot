namespace RobinHood70.HoodBot
{
	using RobinHood70.HoodBot.ViewModel;

	internal interface IParameterFetcher
	{
		void GetParameter(ConstructorParameter parameter);

		void SetParameter(ConstructorParameter parameter);
	}
}