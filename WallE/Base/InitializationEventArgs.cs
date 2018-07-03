﻿namespace RobinHood70.WallE.Base
{
	using System;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WallE.Properties.Messages;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Stores the inputs and the responses for any requests made to the wiki during the initialization routine. This potentially allows requests to be combined between layers.</summary>
	/// <seealso cref="EventArgs" />
	public class InitializationEventArgs : EventArgs
	{
		private Filter filterLocalInterwiki;
		private string interwikiLanguageCode;
		private SiteInfoProperties properties;
		private bool showAllDatabases;
		private bool showNumberInGroup;

		/// <summary>Initializes a new instance of the <see cref="InitializationEventArgs"/> class.</summary>
		/// <param name="input">The SiteInfo input.</param>
		/// <param name="result">The SiteInfo result.</param>
		public InitializationEventArgs(SiteInfoInput input, SiteInfoResult result)
		{
			ThrowNull(input, nameof(input));
			this.filterLocalInterwiki = input.FilterLocalInterwiki;
			this.interwikiLanguageCode = input.InterwikiLanguageCode;
			this.properties = input.Properties;
			this.showAllDatabases = input.ShowAllDatabases;
			this.showNumberInGroup = input.ShowNumberInGroup;
			this.Result = result;
		}

		/// <summary>Gets or sets the filter for local interwikis.</summary>
		/// <value>The filter for local interwikis.</value>
		public Filter FilterLocalInterwiki
		{
			get => this.filterLocalInterwiki;
			set => this.filterLocalInterwiki = this.filterLocalInterwiki != value ? Filter.Any : value;
		}

		/// <summary>Gets or sets the interwiki language code.</summary>
		/// <value>The interwiki language code.</value>
		/// <exception cref="InvalidOperationException">Thrown if a value is specified for the interwiki language code, and another part of the application has already set a different value.</exception>
		public string InterwikiLanguageCode
		{
			get => this.interwikiLanguageCode;
			set => this.interwikiLanguageCode = string.IsNullOrEmpty(this.interwikiLanguageCode) || this.interwikiLanguageCode == value ? value : throw new InvalidOperationException(CurrentCulture(SiteInfoLanguageConflict));
		}

		/// <summary>Gets or sets the properties specify the data to retrieve.</summary>
		/// <value>The properties specify the data to retrieve.</value>
		public SiteInfoProperties Properties
		{
			get => this.properties;
			set => this.properties |= value;
		}

		/// <summary>Gets or sets a value indicating whether to return information about all databases in a <see cref="SiteInfoProperties.DbReplLag"/> request.</summary>
		/// <value><c>true</c> if all databases should be returned; otherwise, <c>false</c>.</value>
		public bool ShowAllDatabases
		{
			get => this.showAllDatabases;
			set => this.showAllDatabases |= value;
		}

		/// <summary>Gets or sets a value indicating whether to show the number of users in each group, and the groups that can be added or removed in a <see cref="SiteInfoProperties.UserGroups"/> request.</summary>
		/// <value><c>true</c> if the number of users in each group and group add/remove rights should be returned; otherwise, <c>false</c>.</value>
		public bool ShowNumberInGroup
		{
			get => this.showNumberInGroup;
			set => this.showNumberInGroup |= value;
		}

		/// <summary>Gets the SiteInfo result.</summary>
		/// <value>The SiteInfo result.</value>
		public SiteInfoResult Result { get; }
	}
}