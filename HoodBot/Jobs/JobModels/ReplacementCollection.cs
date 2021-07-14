namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;

	public sealed class ReplacementCollection : KeyedCollection<Title, Replacement>
	{
		#region Fields
		private readonly bool useTo;
		#endregion

		#region Constructors
		public ReplacementCollection()
			: this(false)
		{
		}

		public ReplacementCollection(bool useTo)
			: base(SimpleTitleEqualityComparer.Instance) => this.useTo = useTo;
		#endregion

		#region Public Properties
		public IEnumerable<Title> Keys
		{
			get
			{
				foreach (var item in this)
				{
					yield return this.GetKeyForItem(item);
				}
			}
		}
		#endregion

		#region Public Methods
		public void Sort() => (this.Items as List<Replacement>)?.Sort((x, y) => SimpleTitleComparer.Instance.Compare(x.From, y.From));
		#endregion

		#region Protected Override Methods
		protected override Title GetKeyForItem(Replacement item) => (item.Actions.HasFlag(ReplacementActions.Move) && this.useTo) ? item.To : item.From;
		#endregion
	}
}