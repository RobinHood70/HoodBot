namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using RobinHood70.CommonCode;
using RobinHood70.WikiCommon;
using static RobinHood70.HoodBot.Jobs.JobModels.CastlesTxInfo;

internal sealed class CastlesTranslator
{
	#region Private Constants
	private const string SpaceSlash = " / ";
	#endregion

	#region Private Static Fields

	// For now, these are static English. Getting them from the files was either non-obvious or more trouble than it was worth.
	private static readonly string[] Relationships = ["Parent", "Child", "Sibling", "Aunt/Uncle", "Nephew/Niece", "Cousin", "Grandparent", "Grandchild", "Spouse", "Friend", "Lover", "Enemy"];
	private static readonly string[] Requesters = ["Ruler", "Requester", "Co-Requester"];
	#endregion

	#region Fields
	private readonly CultureInfo culture;
	private readonly Dictionary<string, string> languageEntries = new(StringComparer.Ordinal);
	private readonly Dictionary<string, string> sentences = new(StringComparer.Ordinal);
	private readonly Dictionary<string, string[]> terms = new(StringComparer.Ordinal);
	private readonly Dictionary<string, string[]> variations = new(StringComparer.Ordinal);
	private readonly Dictionary<bool, ArticleInfo> articleInfos = [];
	#endregion

	#region Constructors
	public CastlesTranslator(CultureInfo culture, Dictionary<string, string> overrides)
	{
		ArgumentNullException.ThrowIfNull(culture);
		this.culture = culture;
		this.ParserOverrides = overrides;
		this.LoadLanguageDatabase();
		this.LoadRules();
		this.LoadSentences();
		this.LoadTerms();
		this.LoadVariations();
	}
	#endregion

	#region Public Properties
	public IDictionary<string, string> ParserOverrides { get; }
	#endregion

	#region Private Properties
	private string LangTag => this.culture.Name;
	#endregion

	#region Public Static Methods
	public static string GetRelatonship(int index) => index < 0 || index >= Relationships.Length
			? throw new ArgumentOutOfRangeException(nameof(index))
			: Relationships[index];

	public static string GetRequester(int index) => index < 0 || index >= Requesters.Length
			? throw new ArgumentOutOfRangeException(nameof(index))
			: Requesters[index];
	#endregion

	#region Public Methods
	public string? GetLanguageEntry(string id, [CallerMemberName] string caller = "")
	{
		if (!this.languageEntries.TryGetValue(id, out var result))
		{
			Debug.WriteLine($"Language entry not found: {id} in {caller}()");
		}

		return result;
	}

	public string? GetSentence([NotNull] string? id, [CallerMemberName] string caller = "")
	{
		ArgumentNullException.ThrowIfNull(id);
		if (!this.sentences.TryGetValue(id, out var value))
		{
			Debug.WriteLine($"Sentence not found: {id} in {caller}()");
			return null;
		}

		return value;
	}

	public string Parse(string input, bool root)
	{
		ArgumentNullException.ThrowIfNull(input);
		var braceSplit = input.Split(TextArrays.CurlyBrackets);

		// Check parentheses before we start modifying braceSplit
		for (var braceIndex = 1; braceIndex < braceSplit.Length; braceIndex += 2)
		{
			var parentheses = braceSplit.Length > 3 || braceSplit[braceIndex - 1].Length > 0 || (braceIndex + 1 < braceSplit.Length && braceSplit[braceIndex + 1].Length > 0);
			var txInfo = new CastlesTxInfo(braceSplit[braceIndex]);
			if (txInfo.Id.Length > 0)
			{
				var list = this.GetList(txInfo);
				var sep = (root && braceSplit.Length == 3 && braceSplit[0].Length == 0 && braceSplit[2].Length == 0)
					? "<newline>"
					: SpaceSlash;
				var newTerm = string.Join(sep, list);
				if (txInfo.Parent.Length > 0)
				{
					newTerm = $"{newTerm} (saved as \"{txInfo.Parent}\")";
					parentheses = true; // If parent exists, force parentheses on
				}
				else
				{
					parentheses &= list.Count > 1; // If single term, turn off parentheses
				}

				if (newTerm.Length > 0)
				{
					if (this.ParserOverrides.TryGetValue(txInfo.Id, out var termOverride))
					{
						newTerm = termOverride.Replace("{0}", newTerm, StringComparison.Ordinal);
					}
					else if (parentheses)
					{
						newTerm = '(' + newTerm + ')';
					}
				}

				braceSplit[braceIndex] = newTerm;
			}
			else if (txInfo.Target.Length > 0)
			{
				braceSplit[braceIndex] = $"(same as \"{txInfo.Target}\", above)";
			}
		}

		var retval = string.Join(string.Empty, braceSplit);
		if (root)
		{
			retval = retval.UpperFirst(this.culture, true);
		}

		return retval;
	}
	#endregion

	#region Private Static Methods
	private static List<string> GetPersonal(CastlesTxInfo txInfo, [CallerMemberName] string caller = "")
	{
		var id = txInfo.Id;
		var value = id switch
		{
			"name.given" => "First Name",
			"name.family" => "Family Name",
			_ => null,
		};

		if (value is null)
		{
			Debug.WriteLine($"Personal not found: {id} in {caller}()");
			return [id];
		}

		value = $"<{txInfo.Target}'s {value}>";
		return [value];
	}
	#endregion

	#region Private Methods
	private static string Capitalize(string retval, Capitalization capitalize, CultureInfo culture)
	{
		return capitalize switch
		{
			Capitalization.None => retval,
			Capitalization.FirstWord => retval.UpperFirst(culture),
			Capitalization.TitleCase => ToTitleCase(retval, culture),
			_ => retval
		};

		static string ToTitleCase(string input, CultureInfo culture)
		{
			var words = input.Split(TextArrays.Space);
			for (var i = 0; i < words.Length; i++)
			{
				words[i] = words[i].UpperFirst(culture);
			}

			return string.Join(' ', words);
		}
	}

	private List<string> GetList(CastlesTxInfo txInfo)
	{
		var list =
			txInfo.Variation ? this.GetVariations(txInfo) :
			txInfo.Term ? this.GetTerm(txInfo.Id, txInfo) :
			txInfo.Personal ? GetPersonal(txInfo) :
			null;
		if (list is null || list.Count == 0)
		{
			throw new InvalidOperationException("Unknown type: " + txInfo.OriginalText);
		}

		for (var listIndex = list.Count - 1; listIndex >= 0; listIndex--)
		{
			if (list[listIndex].Length == 0)
			{
				list.RemoveAt(listIndex);
			}
		}

		return list;
	}

	private List<string>? GetTerm(string id, CastlesTxInfo txInfo, [CallerMemberName] string caller = "")
	{
		if (!this.terms.TryGetValue(id, out var term))
		{
			Debug.WriteLine($"Term not found: {id} in {caller}()");
			return null;
		}

		bool[] include =
		[
			txInfo.Male != false && txInfo.Singular != false,
			txInfo.Male != false && txInfo.Singular != true,
			txInfo.Male != true && txInfo.Singular != false,
			txInfo.Male != true && txInfo.Singular != true,
		];

		var list = new List<string>();
		for (var i = 0; i < 4; i++)
		{
			var word = term[i];
			if (txInfo.ArticleType is bool articleType)
			{
				word = this.articleInfos[articleType].AddArticleToWord(word, txInfo);
			}

			word = Capitalize(word, txInfo.Capitalize, this.culture);
			if (include[i] && !list.Contains(word, StringComparer.Ordinal))
			{
				list.Add(word);
			}
		}

		return list;
	}

	private List<string>? GetVariations(CastlesTxInfo txInfo)
	{
		if (!this.variations.TryGetValue(txInfo.Id, out var variations))
		{
			return null;
		}

		var retval = new List<string>();
		if (txInfo.Sentence)
		{
			foreach (var variation in variations)
			{
				if (this.GetSentence(variation) is string value)
				{
					value = this.Parse(value, false);
					retval.Add(value);
				}
			}
		}
		else if (txInfo.Term)
		{
			foreach (var variation in variations)
			{
				if (this.GetTerm(variation, txInfo) is List<string> value)
				{
					retval.AddRange(value);
				}
			}
		}
		else
		{
			Debug.WriteLine("Unknown variation type: " + txInfo.OriginalText);
		}

		return retval;
	}

	private void LoadLanguageDatabase()
	{
		var obj = JsonShortcuts.Load(GameInfo.Castles.ModFolder + $"LanguageDatabase_Gameplay_{this.LangTag}.json");
		var items = obj.MustHave("entries");
		foreach (var item in items)
		{
			this.languageEntries.TryAdd(item.MustHaveString("key"), item.MustHaveString("entry"));
		}
	}

	private void LoadRules()
	{
		var obj = JsonShortcuts.Load(GameInfo.Castles.ModFolder + $"rules_{this.LangTag}.json");
		var items = obj.MustHave("_rawData");
		var rules = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (var item in items)
		{
			var key = item.MustHaveString("_referenceId");
			var value = item.MustHaveString("_data");
			rules.Add(key, value);
		}

		// Indefinite articles
		var articleInfo = new ArticleInfo(
			rules["article.indefinite.format"],
			rules["article.indefinite.starts"],
			rules["article.indefinite.exception.format"],
			rules["article.indefinite.exception.starts"]);
		this.articleInfos.Add(false, articleInfo);

		// Definite articles
		articleInfo = new ArticleInfo(
			rules["article.definite.format"],
			rules["article.definite.starts"],
			rules["article.definite.exception.format"],
			rules["article.definite.exception.starts"]);
		this.articleInfos.Add(true, articleInfo);
	}

	private void LoadSentences()
	{
		var obj = JsonShortcuts.Load(GameInfo.Castles.ModFolder + $"sentences_{this.LangTag}.json");
		var items = obj.MustHave("_rawData");
		foreach (var item in items)
		{
			this.sentences.Add(item.MustHaveString("_referenceId"), item.MustHaveString("_data"));
		}
	}

	private void LoadTerms()
	{
		var obj = JsonShortcuts.Load(GameInfo.Castles.ModFolder + $"terms_{this.LangTag}.json");
		var items = obj.MustHave("_rawData");
		foreach (var item in items)
		{
			var data = item.MustHaveString("_data");
			var split = data.Split(TextArrays.Pipe);
			this.terms.Add(item.MustHaveString("_referenceId"), split);
		}
	}

	private void LoadVariations()
	{
		var obj = JsonShortcuts.Load(GameInfo.Castles.ModFolder + $"variations_{this.LangTag}.json");
		var items = obj.MustHave("_rawData");
		foreach (var item in items)
		{
			var data = item.MustHaveString("_data");
			var split = data.Split(TextArrays.Comma);
			var varList = new List<string>(split.Length);
			foreach (var id in split)
			{
				// Square brackets appear to indicate randomness weight.
				var stripped = id.Split('[', 2)[0];
				varList.Add(stripped);
			}

			this.variations.Add(item.MustHaveString("_referenceId"), [.. varList]);
		}
	}
	#endregion

	#region Private Classes
	private sealed class ArticleInfo(string articles, string articleStarts, string exceptions, string exceptionStarts)
	{
		#region Constants
		private const string MatchAnything = "";
		private const string MatchNothing = "_^";
		#endregion

		#region Public Properties
		public string[] Articles { get; } = articles.Split(TextArrays.Pipe);

		public Regex ArticleStarts { get; } = new Regex(articleStarts.Length == 0 ? MatchAnything : $@"\A({articleStarts})", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);

		public string[] Exceptions { get; } = exceptions.Split(TextArrays.Pipe);

		public Regex ExceptionStarts { get; } = new Regex(exceptionStarts.Length == 0 ? MatchNothing : $@"\A({exceptionStarts})", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
		#endregion

		#region Public Methods
		public string AddArticleToWord(string input, CastlesTxInfo txInfo)
		{
			var declension = (txInfo.Male == false ? 2 : 0) + (txInfo.Singular == false ? 1 : 0);

			// Checks must be in this exact order - do not optimize.
			var retval =
				this.ExceptionStarts.Match(input).Success ? this.Exceptions[declension] :
				this.ArticleStarts.Match(input).Success ? this.Articles[declension] :
				this.Exceptions[declension];
			return retval.Replace("{0}", input, StringComparison.Ordinal);
		}
		#endregion
	}
	#endregion
}