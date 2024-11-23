namespace RobinHood70.HoodBot.ViewModels;

using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using RobinHood70.HoodBot.Models;

// TODO: Spit into separate model and viewmodel. Model should take care of saving/loading data (or use HostBuilder in App).
public sealed class WikiInfoViewModel : ObservableObject, IEditableObject
{
	#region Fields
	private Uri? api;
	private string? displayName;
	private string? logPage;
	private int? maxLag;
	private string? password;
	private int? readThrottling;
	private string? resultPage;
	private string? siteClassIdentifier;
	private string? userName;
	private int? writeThrottling;
	#endregion

	#region Constructors
	public WikiInfoViewModel()
		: this(new WikiInfo())
	{
	}

	public WikiInfoViewModel(WikiInfo wikiInfo)
	{
		ArgumentNullException.ThrowIfNull(wikiInfo);
		this.WikiInfo = wikiInfo;
		this.CancelEdit();
	}
	#endregion

	#region Public Properties
	public Uri? Api
	{
		get => this.api;
		set => this.SetProperty(ref this.api, value);
	}

	public string? DisplayName
	{
		get => this.displayName;
		set => this.SetProperty(ref this.displayName, value);
	}

	public bool IsValid => !string.IsNullOrWhiteSpace(this.DisplayName) && this.Api?.IsWellFormedOriginalString() == true;

	public string? LogPage
	{
		get => this.logPage;
		set => this.SetProperty(ref this.logPage, value);
	}

	public int? MaxLag
	{
		get => this.maxLag;
		set => this.SetProperty(ref this.maxLag, value);
	}

	public string? Password
	{
		get => this.password;
		set => this.SetProperty(ref this.password, value);
	}

	public int? ReadThrottling
	{
		get => this.readThrottling;
		set => this.SetProperty(ref this.readThrottling, value);
	}

	public string? ResultsPage
	{
		get => this.resultPage;
		set => this.SetProperty(ref this.resultPage, value);
	}

	public string? SiteClassIdentifier
	{
		get => this.siteClassIdentifier;
		set => this.SetProperty(ref this.siteClassIdentifier, value);
	}

	public string? UserName
	{
		get => this.userName;
		set => this.SetProperty(ref this.userName, value);
	}

	public WikiInfo WikiInfo { get; }

	public int? WriteThrottling
	{
		get => this.writeThrottling;
		set => this.SetProperty(ref this.writeThrottling, value);
	}
	#endregion

	#region Public Methods
	public void BeginEdit()
	{
		// Required for interface but nothing needs to be done.
	}

	public void CancelEdit()
	{
		this.Api = this.WikiInfo.Api;
		this.DisplayName = this.WikiInfo.DisplayName;
		this.LogPage = this.WikiInfo.LogPage;
		this.MaxLag = this.WikiInfo.MaxLag;
		this.Password = this.WikiInfo.Password;
		this.ReadThrottling = this.WikiInfo.ReadThrottling;
		this.ResultsPage = this.WikiInfo.ResultsPage;
		this.SiteClassIdentifier = this.WikiInfo.SiteClassIdentifier;
		this.UserName = this.WikiInfo.UserName;
		this.WriteThrottling = this.WikiInfo.WriteThrottling;
	}

	public void EndEdit()
	{
		this.WikiInfo.Api = this.Api;
		this.WikiInfo.DisplayName = string.IsNullOrWhiteSpace(this.DisplayName)
			? null
			: this.DisplayName;
		this.WikiInfo.LogPage = string.IsNullOrWhiteSpace(this.LogPage)
			? null
			: this.LogPage;
		this.WikiInfo.MaxLag = this.MaxLag;
		this.WikiInfo.Password = string.IsNullOrWhiteSpace(this.Password)
			? null
			: this.Password;
		this.WikiInfo.ReadThrottling = this.ReadThrottling;
		this.WikiInfo.ResultsPage = string.IsNullOrWhiteSpace(this.ResultsPage)
			? null
			: this.ResultsPage;
		this.WikiInfo.SiteClassIdentifier = string.IsNullOrWhiteSpace(this.SiteClassIdentifier)
			? null
			: this.SiteClassIdentifier;
		this.WikiInfo.UserName = string.IsNullOrWhiteSpace(this.UserName)
			? null
			: this.UserName;
		this.WikiInfo.WriteThrottling = this.WriteThrottling;
	}
	#endregion

	#region Public Override Methods
	public override string ToString() => this.DisplayName ?? this.GetType().Name;
	#endregion
}