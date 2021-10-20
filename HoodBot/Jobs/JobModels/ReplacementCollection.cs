namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using RobinHood70.Robby.Design;

	internal sealed class ReplacementCollection : KeyedCollection<ISimpleTitle, Replacement>
	{
		#region Constructors
		public ReplacementCollection()
			: base(SimpleTitleEqualityComparer.Instance)
		{
		}
		#endregion

		#region Public Methods
		public void Sort() => (this.Items as List<Replacement>)?.Sort((x, y) => SimpleTitleComparer.Instance.Compare(x.From, y.From));
		#endregion

		#region Protected Override Methods
		protected override ISimpleTitle GetKeyForItem(Replacement item) => item.From;
		#endregion
	}
}