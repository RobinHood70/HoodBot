namespace RobinHood70.HoodBot.Jobs.Design
{
	using System;
	using RobinHood70.HoodBot.Properties;
	using RobinHood70.Robby;

	/// <summary>Implements the <see cref="ResultHandler" /> class and e-mails results to the user if they have e-mail enabled.</summary>
	/// <seealso cref="ResultHandler" />
	public class EmailResultHandler : ResultHandler
	{
		#region Fields
		private readonly User user;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="EmailResultHandler"/> class.</summary>
		/// <param name="user">The user whose talk page should be added to.</param>
		public EmailResultHandler(User user)
			: base(user?.Title.Site.Culture)
		{
			ArgumentNullException.ThrowIfNull(user);
			user.LoadUserInfo();
			if (user.Info?.Emailable == false)
			{
				throw new InvalidOperationException(Resources.UserEmailDisabled);
			}

			this.user = user;
		}
		#endregion

		#region Public Override Methods

		/// <inheritdoc/>
		public override void Save()
		{
			if (this.StringBuilder.Length > 0)
			{
				this.user.Email(this.Description, this.StringBuilder.ToString(), false);
			}
		}
		#endregion
	}
}