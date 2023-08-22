namespace RobinHood70.HoodBot.Jobs.Design
{
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;

	/// <summary>Implements the <see cref="ResultHandler" /> class and saves results to a new section of a user's talk page.</summary>
	/// <seealso cref="ResultHandler" />
	public class UserTalkResultHandler : ResultHandler
	{
		#region Fields
		private readonly string botTalkSummary;
		private readonly User user;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="UserTalkResultHandler"/> class.</summary>
		/// <param name="user">The user whose talk page should be added to.</param>
		public UserTalkResultHandler(User user)
			: base(user.NotNull().Title.Site.Culture)
		{
			this.user = user;
			this.botTalkSummary = this.ResourceManager.GetString("BotJobNotice", this.Culture) ?? this.DefaultText;
		}
		#endregion

		#region Public Override Methods

		/// <inheritdoc/>
		public override void Save()
		{
			if (this.StringBuilder.Length > 0)
			{
				this.user.NewTalkPageMessage(this.Description, this.StringBuilder.ToString(), this.botTalkSummary);
			}
		}
		#endregion
	}
}