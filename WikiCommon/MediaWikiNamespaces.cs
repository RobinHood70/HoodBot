#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member - self-documenting
namespace RobinHood70.WikiCommon
{
	/// <summary>This class acts as an Enum, but with no need for casting to and from ints for Namespace IDs. Similar classes can be created for specific wikis, if desired.</summary>
	public static class MediaWikiNamespaces
	{
		public const int Category = 14;
		public const int CategoryTalk = 15;
		public const int File = 6;
		public const int FileTalk = 7;
		public const int Help = 12;
		public const int HelpTalk = 13;
		public const int Image = 6;
		public const int ImageTalk = 7;
		public const int Main = 0;
		public const int Media = -2;
		public const int MediaWiki = 8;
		public const int MediaWikiTalk = 9;
		public const int Project = 4;
		public const int ProjectTalk = 5;
		public const int Special = -1;
		public const int Talk = 1;
		public const int Template = 10;
		public const int TemplateTalk = 11;
		public const int User = 2;
		public const int UserTalk = 3;
	}
}