namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System.Collections.Generic;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Base;
	using static RobinHood70.CommonCode.Globals;

	public class ProtectedPageCreator : PageCreator
	{
		public override Page CreatePage(ISimpleTitle title) => new ProtectedPage(title);

		protected override void AddCustomPropertyInputs(IList<IPropertyInput> propertyInputs)
		{
			ThrowNull(propertyInputs, nameof(propertyInputs));
			foreach (var prop in propertyInputs)
			{
				if (prop is InfoInput infoExists)
				{
					infoExists.Properties |= InfoProperties.Protection;
					return;
				}
			}

			propertyInputs.Add(new InfoInput() { Properties = InfoProperties.Protection });
		}
	}
}