namespace RobinHood70.HoodBot.Uesp;

public sealed class NsinfoItem(string baseName, string category, string full, string icon, string iconUrl, string id, bool isGamespace, bool isModspace, bool isPseudospace, string mainPage, string modName, string modParent, string name, int nsId, string pageName, string parent, string trail)
{
	#region Public Properties
	public string Base { get; } = baseName;

	public string Category { get; } = category;

	public string Full { get; } = full;

	public string Icon { get; } = icon;

	public string IconUrl { get; } = iconUrl;

	public string Id { get; } = id;

	public bool IsGamespace { get; } = isGamespace;

	public bool IsModspace { get; } = isModspace;

	public bool IsPseudospace { get; } = isPseudospace;

	public string MainPage { get; } = mainPage;

	public string ModName { get; } = modName;

	public string ModParent { get; } = modParent;

	public string Name { get; } = name;

	public int NsId { get; } = nsId;

	public string PageName { get; } = pageName;

	public string Parent { get; } = parent;

	public string Trail { get; } = trail;
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Base;
	#endregion
}